using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfClientPortalPasswordResetRepository : IClientPortalPasswordResetRepository
{
    private readonly ControlCloudDbContext _dbContext;
    public EfClientPortalPasswordResetRepository(ControlCloudDbContext dbContext) => _dbContext = dbContext;

    public async Task<ControlCloudClientPortalPasswordReset?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientPortalPasswordResets.AsNoTracking()
            .SingleOrDefaultAsync(reset => reset.TokenHash == tokenHash, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public Task AddAsync(ControlCloudClientPortalPasswordReset passwordReset, CancellationToken cancellationToken = default) =>
        _dbContext.ClientPortalPasswordResets.AddAsync(ToEntity(passwordReset), cancellationToken).AsTask();

    public async Task SaveAsync(ControlCloudClientPortalPasswordReset passwordReset, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientPortalPasswordResets
            .SingleAsync(reset => reset.PasswordResetId == passwordReset.PasswordResetId, cancellationToken);
        Apply(passwordReset, entity);
        _dbContext.Entry(entity).Property(value => value.ConcurrencyToken).OriginalValue =
            passwordReset.OriginalConcurrencyToken;
    }

    private static ControlCloudClientPortalPasswordReset ToDomain(ControlCloudClientPortalPasswordResetEntity entity) => new()
    {
        PasswordResetId = entity.PasswordResetId,
        UserId = entity.UserId,
        ClientId = entity.ClientId,
        TokenHash = entity.TokenHash,
        CreatedAtUtc = entity.CreatedAtUtc,
        ExpiresAtUtc = entity.ExpiresAtUtc,
        UsedAtUtc = entity.UsedAtUtc,
        ConcurrencyToken = entity.ConcurrencyToken,
        OriginalConcurrencyToken = entity.ConcurrencyToken
    };

    private static ControlCloudClientPortalPasswordResetEntity ToEntity(ControlCloudClientPortalPasswordReset reset)
    {
        var entity = new ControlCloudClientPortalPasswordResetEntity();
        Apply(reset, entity);
        return entity;
    }

    private static void Apply(ControlCloudClientPortalPasswordReset source, ControlCloudClientPortalPasswordResetEntity target)
    {
        target.PasswordResetId = source.PasswordResetId;
        target.UserId = source.UserId;
        target.ClientId = source.ClientId;
        target.TokenHash = source.TokenHash;
        target.CreatedAtUtc = source.CreatedAtUtc;
        target.ExpiresAtUtc = source.ExpiresAtUtc;
        target.UsedAtUtc = source.UsedAtUtc;
        target.ConcurrencyToken = source.ConcurrencyToken;
    }
}
