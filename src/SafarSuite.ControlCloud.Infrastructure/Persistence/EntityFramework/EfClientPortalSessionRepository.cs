using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientPortalSessionRepository : IClientPortalSessionRepository
{
    private readonly ControlCloudDbContext _dbContext;

    public EfClientPortalSessionRepository(ControlCloudDbContext dbContext) => _dbContext = dbContext;

    public async Task<ControlCloudClientPortalSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientPortalSessions.AsNoTracking()
            .SingleOrDefaultAsync(session => session.SessionId == sessionId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<ControlCloudClientPortalSession?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientPortalSessions.AsNoTracking()
            .SingleOrDefaultAsync(
                session => session.RefreshTokenHash == refreshTokenHash
                    || session.PreviousRefreshTokenHash == refreshTokenHash,
                cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyCollection<ControlCloudClientPortalSession>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClientPortalSessions.AsNoTracking()
            .Where(session => session.UserId == userId)
            .Select(session => ToDomain(session))
            .ToArrayAsync(cancellationToken);
    }

    public Task AddAsync(ControlCloudClientPortalSession session, CancellationToken cancellationToken = default) =>
        _dbContext.ClientPortalSessions.AddAsync(ToEntity(session), cancellationToken).AsTask();

    public async Task SaveAsync(ControlCloudClientPortalSession session, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientPortalSessions
            .SingleAsync(stored => stored.SessionId == session.SessionId, cancellationToken);
        Apply(session, entity);
        _dbContext.Entry(entity).Property(value => value.ConcurrencyToken).OriginalValue =
            session.OriginalConcurrencyToken;
    }

    private static ControlCloudClientPortalSession ToDomain(ControlCloudClientPortalSessionEntity entity) => new()
    {
        SessionId = entity.SessionId,
        UserId = entity.UserId,
        ClientId = entity.ClientId,
        Role = entity.Role,
        SecurityVersion = entity.SecurityVersion,
        RefreshTokenHash = entity.RefreshTokenHash,
        PreviousRefreshTokenHash = entity.PreviousRefreshTokenHash,
        CreatedAtUtc = entity.CreatedAtUtc,
        LastActivityAtUtc = entity.LastActivityAtUtc,
        IdleExpiresAtUtc = entity.IdleExpiresAtUtc,
        AbsoluteExpiresAtUtc = entity.AbsoluteExpiresAtUtc,
        RevokedAtUtc = entity.RevokedAtUtc,
        RevokedReason = entity.RevokedReason,
        ConcurrencyToken = entity.ConcurrencyToken,
        OriginalConcurrencyToken = entity.ConcurrencyToken
    };

    private static ControlCloudClientPortalSessionEntity ToEntity(ControlCloudClientPortalSession session)
    {
        var entity = new ControlCloudClientPortalSessionEntity();
        Apply(session, entity);
        return entity;
    }

    private static void Apply(ControlCloudClientPortalSession source, ControlCloudClientPortalSessionEntity target)
    {
        target.SessionId = source.SessionId;
        target.UserId = source.UserId;
        target.ClientId = source.ClientId;
        target.Role = source.Role;
        target.SecurityVersion = source.SecurityVersion;
        target.RefreshTokenHash = source.RefreshTokenHash;
        target.PreviousRefreshTokenHash = source.PreviousRefreshTokenHash;
        target.CreatedAtUtc = source.CreatedAtUtc;
        target.LastActivityAtUtc = source.LastActivityAtUtc;
        target.IdleExpiresAtUtc = source.IdleExpiresAtUtc;
        target.AbsoluteExpiresAtUtc = source.AbsoluteExpiresAtUtc;
        target.RevokedAtUtc = source.RevokedAtUtc;
        target.RevokedReason = source.RevokedReason;
        target.ConcurrencyToken = source.ConcurrencyToken;
    }
}
