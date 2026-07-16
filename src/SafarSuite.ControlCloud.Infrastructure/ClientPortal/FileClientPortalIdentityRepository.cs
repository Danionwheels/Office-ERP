using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileClientPortalIdentityRepository : IClientPortalIdentityRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileClientPortalIdentityRepository(ClientPortalAccessOptions options)
    {
        _storePath = ResolveStorePath(options.IdentityStorePath);
    }

    public async Task<ControlCloudClientPortalInvitation?> GetInvitationByIdAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var store = await ReadStoreAsync(cancellationToken);

            return store.Invitations.FirstOrDefault(invitation => invitation.InvitationId == invitationId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ControlCloudClientPortalInvitation?> GetInvitationByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var store = await ReadStoreAsync(cancellationToken);

            return store.Invitations.FirstOrDefault(invitation => invitation.TokenHash == tokenHash);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyCollection<ControlCloudClientPortalInvitation>> ListInvitationsByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var store = await ReadStoreAsync(cancellationToken);

            return store.Invitations
                .Where(invitation => invitation.ClientId == clientId)
                .OrderByDescending(invitation => invitation.InvitedAtUtc)
                .ThenBy(invitation => invitation.Email)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddInvitationAsync(
        ControlCloudClientPortalInvitation invitation,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var store = await ReadStoreAsync(cancellationToken);
            store.Invitations.RemoveAll(stored => stored.InvitationId == invitation.InvitationId);
            store.Invitations.Add(invitation);
            await WriteStoreAsync(store, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task SaveInvitationAsync(
        ControlCloudClientPortalInvitation invitation,
        CancellationToken cancellationToken = default)
    {
        return AddInvitationAsync(invitation, cancellationToken);
    }

    public async Task<ControlCloudClientPortalUser?> GetUserByClientAndEmailAsync(
        Guid clientId,
        string email,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var normalizedEmail = ControlCloudClientPortalInvitation.NormalizeEmail(email);
            var store = await ReadStoreAsync(cancellationToken);

            return PrepareLoadedUser(store.Users.FirstOrDefault(user =>
                user.ClientId == clientId
                && ControlCloudClientPortalInvitation.NormalizeEmail(user.Email) == normalizedEmail));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ControlCloudClientPortalUser?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var store = await ReadStoreAsync(cancellationToken);
            return PrepareLoadedUser(store.Users.FirstOrDefault(user => user.UserId == userId));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddUserAsync(
        ControlCloudClientPortalUser user,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var store = await ReadStoreAsync(cancellationToken);
            store.Users.RemoveAll(stored => stored.UserId == user.UserId);
            store.Users.Add(user);
            await WriteStoreAsync(store, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveUserAsync(
        ControlCloudClientPortalUser user,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var store = await ReadStoreAsync(cancellationToken);
            var index = store.Users.FindIndex(stored => stored.UserId == user.UserId);

            if (index < 0)
            {
                throw new InvalidOperationException($"Client Portal user '{user.UserId}' was not found.");
            }

            var storedToken = store.Users[index].ConcurrencyToken;

            if (storedToken != user.OriginalConcurrencyToken)
            {
                throw new InvalidOperationException("Client Portal user security state changed concurrently.");
            }

            user.OriginalConcurrencyToken = user.ConcurrencyToken;
            store.Users[index] = user;
            await WriteStoreAsync(store, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ClientPortalIdentityStore> ReadStoreAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return new ClientPortalIdentityStore();
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        return await JsonSerializer.DeserializeAsync<ClientPortalIdentityStore>(
            stream,
            JsonOptions,
            cancellationToken)
            ?? new ClientPortalIdentityStore();
    }

    private async Task WriteStoreAsync(
        ClientPortalIdentityStore store,
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

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/client-portal-identities.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private static ControlCloudClientPortalUser? PrepareLoadedUser(ControlCloudClientPortalUser? user)
    {
        if (user is not null)
        {
            user.OriginalConcurrencyToken = user.ConcurrencyToken;
        }

        return user;
    }

    private sealed class ClientPortalIdentityStore
    {
        public List<ControlCloudClientPortalInvitation> Invitations { get; set; } = [];

        public List<ControlCloudClientPortalUser> Users { get; set; } = [];
    }
}
