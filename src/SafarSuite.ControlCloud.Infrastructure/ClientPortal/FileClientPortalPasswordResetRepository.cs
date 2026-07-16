using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileClientPortalPasswordResetRepository : IClientPortalPasswordResetRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;
    public FileClientPortalPasswordResetRepository(ClientPortalAccessOptions options) => _storePath = Resolve(options.PasswordResetStorePath);

    public async Task<ControlCloudClientPortalPasswordReset?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var reset = (await ReadAsync(cancellationToken)).FirstOrDefault(value => value.TokenHash == tokenHash);
            if (reset is not null) reset.OriginalConcurrencyToken = reset.ConcurrencyToken;
            return reset;
        }
        finally { _gate.Release(); }
    }

    public async Task AddAsync(ControlCloudClientPortalPasswordReset passwordReset, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var resets = await ReadAsync(cancellationToken);
            if (resets.Any(stored => stored.PasswordResetId == passwordReset.PasswordResetId))
            {
                throw new InvalidOperationException($"Password reset '{passwordReset.PasswordResetId}' already exists.");
            }
            passwordReset.OriginalConcurrencyToken = passwordReset.ConcurrencyToken;
            resets.Add(passwordReset);
            await WriteAsync(resets, cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task SaveAsync(ControlCloudClientPortalPasswordReset passwordReset, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var resets = await ReadAsync(cancellationToken);
            var index = resets.FindIndex(stored => stored.PasswordResetId == passwordReset.PasswordResetId);
            if (index < 0) throw new InvalidOperationException($"Password reset '{passwordReset.PasswordResetId}' was not found.");
            if (resets[index].ConcurrencyToken != passwordReset.OriginalConcurrencyToken)
            {
                throw new InvalidOperationException("Password reset state changed concurrently.");
            }
            passwordReset.OriginalConcurrencyToken = passwordReset.ConcurrencyToken;
            resets[index] = passwordReset;
            await WriteAsync(resets, cancellationToken);
        }
        finally { _gate.Release(); }
    }

    private async Task<List<ControlCloudClientPortalPasswordReset>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath)) return [];
        await using var stream = File.OpenRead(_storePath);
        var resets = await JsonSerializer.DeserializeAsync<List<ControlCloudClientPortalPasswordReset>>(stream, JsonOptions, cancellationToken) ?? [];
        foreach (var reset in resets) reset.OriginalConcurrencyToken = reset.ConcurrencyToken;
        return resets;
    }

    private async Task WriteAsync(List<ControlCloudClientPortalPasswordReset> resets, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
        await using var stream = new FileStream(_storePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, resets, JsonOptions, cancellationToken);
    }

    private static string Resolve(string path) => Path.IsPathRooted(path)
        ? path
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
}
