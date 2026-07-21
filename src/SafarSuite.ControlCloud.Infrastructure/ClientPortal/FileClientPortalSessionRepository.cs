using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileClientPortalSessionRepository : IClientPortalSessionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileClientPortalSessionRepository(ClientPortalAccessOptions options) =>
        _storePath = Resolve(options.SessionStorePath);

    public Task<ControlCloudClientPortalSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        ReadOneAsync(session => session.SessionId == sessionId, cancellationToken);

    public Task<ControlCloudClientPortalSession?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default) =>
        ReadOneAsync(session => session.RefreshTokenHash == refreshTokenHash || session.PreviousRefreshTokenHash == refreshTokenHash, cancellationToken);

    public async Task<IReadOnlyCollection<ControlCloudClientPortalSession>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { return (await ReadAsync(cancellationToken)).Where(session => session.UserId == userId).ToArray(); }
        finally { _gate.Release(); }
    }

    public async Task AddAsync(ControlCloudClientPortalSession session, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var sessions = await ReadAsync(cancellationToken);
            if (sessions.Any(stored => stored.SessionId == session.SessionId))
            {
                throw new InvalidOperationException($"Client Portal session '{session.SessionId}' already exists.");
            }
            session.OriginalConcurrencyToken = session.ConcurrencyToken;
            sessions.Add(session);
            await WriteAsync(sessions, cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task SaveAsync(ControlCloudClientPortalSession session, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var sessions = await ReadAsync(cancellationToken);
            var index = sessions.FindIndex(stored => stored.SessionId == session.SessionId);
            if (index < 0) throw new InvalidOperationException($"Client Portal session '{session.SessionId}' was not found.");
            if (sessions[index].ConcurrencyToken != session.OriginalConcurrencyToken)
            {
                throw new InvalidOperationException("Client Portal session state changed concurrently.");
            }
            session.OriginalConcurrencyToken = session.ConcurrencyToken;
            sessions[index] = session;
            await WriteAsync(sessions, cancellationToken);
        }
        finally { _gate.Release(); }
    }

    private async Task<ControlCloudClientPortalSession?> ReadOneAsync(Func<ControlCloudClientPortalSession, bool> predicate, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var session = (await ReadAsync(cancellationToken)).FirstOrDefault(predicate);
            if (session is not null) session.OriginalConcurrencyToken = session.ConcurrencyToken;
            return session;
        }
        finally { _gate.Release(); }
    }

    private async Task<List<ControlCloudClientPortalSession>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath)) return [];
        await using var stream = File.OpenRead(_storePath);
        var sessions = await JsonSerializer.DeserializeAsync<List<ControlCloudClientPortalSession>>(stream, JsonOptions, cancellationToken) ?? [];
        foreach (var session in sessions) session.OriginalConcurrencyToken = session.ConcurrencyToken;
        return sessions;
    }

    private async Task WriteAsync(List<ControlCloudClientPortalSession> sessions, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
        await using var stream = new FileStream(_storePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, sessions, JsonOptions, cancellationToken);
    }

    private static string Resolve(string path) => Path.IsPathRooted(path)
        ? path
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
}
