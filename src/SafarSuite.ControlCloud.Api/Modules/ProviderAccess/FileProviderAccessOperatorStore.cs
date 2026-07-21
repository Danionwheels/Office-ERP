using System.Text.Json;
using SafarSuite.ControlCloud.Api.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public sealed class FileProviderAccessOperatorStore : IProviderAccessOperatorStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ClientPortalProviderAccessOptions _options;
    private readonly string _storePath;

    public FileProviderAccessOperatorStore(ClientPortalProviderAccessOptions options)
    {
        _options = options;
        _storePath = ResolveStorePath(options.OperatorStorePath);
    }

    public async Task<IReadOnlyCollection<ProviderAccessOperator>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var store = await ReadStoreAsync(cancellationToken);

            return store.Operators
                .OrderBy(providerOperator => providerOperator.Email, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ProviderAccessOperator?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var normalizedUserId = userId.Trim();
            var store = await ReadStoreAsync(cancellationToken);

            return store.Operators.FirstOrDefault(providerOperator =>
                providerOperator.UserId.Equals(normalizedUserId, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ProviderAccessOperator?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var normalizedEmail = NormalizeEmail(email);
            var store = await ReadStoreAsync(cancellationToken);

            return store.Operators.FirstOrDefault(providerOperator =>
                NormalizeEmail(providerOperator.Email).Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        ProviderAccessOperator providerOperator,
        CancellationToken cancellationToken = default)
    {
        EnsureSupportedScopes(providerOperator.Scopes, providerOperator.Email);

        await _gate.WaitAsync(cancellationToken);

        try
        {
            var store = await ReadStoreAsync(cancellationToken);
            var normalizedEmail = NormalizeEmail(providerOperator.Email);

            store.Operators.RemoveAll(stored =>
                stored.UserId.Equals(providerOperator.UserId, StringComparison.OrdinalIgnoreCase)
                || NormalizeEmail(stored.Email).Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
            store.Operators.Add(providerOperator);

            await WriteStoreAsync(store, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ProviderAccessOperatorStore> ReadStoreAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            var seededStore = CreateSeededStore();
            await WriteStoreAsync(seededStore, cancellationToken);

            return seededStore;
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        var store = await JsonSerializer.DeserializeAsync<ProviderAccessOperatorStore>(
            stream,
            JsonOptions,
            cancellationToken)
            ?? new ProviderAccessOperatorStore();

        if (store.Operators.Count == 0 && _options.Users.Length > 0)
        {
            store = CreateSeededStore();
            await WriteStoreAsync(store, cancellationToken);
        }

        return store;
    }

    private async Task WriteStoreAsync(
        ProviderAccessOperatorStore store,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_storePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);

        await JsonSerializer.SerializeAsync(stream, store, JsonOptions, cancellationToken);
    }

    private ProviderAccessOperatorStore CreateSeededStore()
    {
        var now = DateTimeOffset.UtcNow;
        var operators = _options.Users
            .Where(user =>
                !string.IsNullOrWhiteSpace(user.Email)
                && !string.IsNullOrWhiteSpace(user.PasswordHash))
            .GroupBy(user => NormalizeEmail(user.Email), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(user => new ProviderAccessOperator
            {
                UserId = string.IsNullOrWhiteSpace(user.UserId)
                    ? Guid.NewGuid().ToString("N")
                    : user.UserId.Trim(),
                Email = NormalizeEmail(user.Email),
                FullName = user.FullName.Trim(),
                PasswordHash = user.PasswordHash.Trim(),
                Status = ProviderAccessOperatorStatuses.IsSupported(user.Status)
                    ? user.Status.Trim()
                    : ProviderAccessOperatorStatuses.Suspended,
                Scopes = NormalizeSeedScopes(user).ToArray(),
                CreatedAtUtc = now,
                CreatedBy = "configuration-seed"
            })
            .ToList();

        return new ProviderAccessOperatorStore
        {
            Operators = operators
        };
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/provider-access-operators.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private static string NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? ""
            : email.Trim().ToLowerInvariant();
    }

    private IReadOnlyCollection<string> NormalizeSeedScopes(ProviderAccessUserOptions user)
    {
        var scopes = ProviderAccessScopes.Normalize(user.Scopes, _options.DefaultScopes);
        EnsureSupportedScopes(scopes, user.Email);

        return scopes;
    }

    private static void EnsureSupportedScopes(
        IEnumerable<string>? scopes,
        string operatorEmail)
    {
        var unsupportedScopes = ProviderAccessScopes.FindUnsupported(scopes);

        if (unsupportedScopes.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Provider access operator '{NormalizeEmail(operatorEmail)}' has unsupported scope(s): {string.Join(", ", unsupportedScopes)}.");
    }

    private sealed class ProviderAccessOperatorStore
    {
        public List<ProviderAccessOperator> Operators { get; set; } = [];
    }
}
