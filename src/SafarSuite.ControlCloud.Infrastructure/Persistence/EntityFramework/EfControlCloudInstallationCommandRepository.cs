using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfControlCloudInstallationCommandRepository
    : IControlCloudInstallationCommandRepository
{
    private readonly ControlCloudDbContext _dbContext;

    public EfControlCloudInstallationCommandRepository(ControlCloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ControlCloudInstallationCommand?> GetByCommandIdAsync(
        Guid commandId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.InstallationCommands
            .SingleOrDefaultAsync(command => command.CommandId == commandId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<ControlCloudInstallationCommand?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var cleanIdempotencyKey = idempotencyKey.Trim();
        var entity = await _dbContext.InstallationCommands
            .SingleOrDefaultAsync(
                command => command.IdempotencyKey == cleanIdempotencyKey,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<long> GetLatestCommandVersionAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();

        return await _dbContext.InstallationCommands
            .Where(command => command.InstallationId == cleanInstallationId)
            .MaxAsync(command => (long?)command.CommandVersion, cancellationToken)
            ?? 0;
    }

    public async Task<IReadOnlyCollection<ControlCloudInstallationCommand>> ListPendingAsync(
        string installationId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();
        var entities = await _dbContext.InstallationCommands
            .Where(command => command.InstallationId == cleanInstallationId)
            .Where(command => command.Status == ControlCloudInstallationCommandStatuses.Pending)
            .Where(command => command.ExpiresAtUtc > asOfUtc)
            .Where(command => command.NotBeforeUtc == null || command.NotBeforeUtc <= asOfUtc)
            .OrderBy(command => command.CommandVersion)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToArray();
    }

    public async Task<ControlCloudInstallationCommand?> GetLatestByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();
        var entity = await _dbContext.InstallationCommands
            .Where(command => command.InstallationId == cleanInstallationId)
            .OrderBy(command => command.CommandVersion)
            .ThenBy(command => command.CommandId)
            .LastOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(
        ControlCloudInstallationCommand command,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.InstallationCommands.AddAsync(
            FromDomain(command),
            cancellationToken);
    }

    public async Task SaveAsync(
        ControlCloudInstallationCommand command,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.InstallationCommands
            .SingleOrDefaultAsync(
                stored => stored.CommandId == command.CommandId,
                cancellationToken);

        if (entity is null)
        {
            await AddAsync(command, cancellationToken);

            return;
        }

        UpdateEntity(entity, command);
    }

    private static ControlCloudInstallationCommand ToDomain(
        ControlCloudInstallationCommandEntity entity)
    {
        return ControlCloudInstallationCommand.Restore(
            entity.CommandId,
            entity.ClientId,
            entity.InstallationId,
            entity.CommandVersion,
            entity.CommandType,
            entity.Status,
            entity.IdempotencyKey,
            entity.PayloadJson,
            entity.SignatureAlgorithm,
            entity.SignatureKeyId,
            entity.PayloadSha256,
            entity.SignatureValue,
            entity.QueuedAtUtc,
            entity.NotBeforeUtc,
            entity.ExpiresAtUtc,
            entity.AcknowledgedAtUtc,
            entity.AcknowledgementStatus,
            entity.AcknowledgementDetail);
    }

    private static ControlCloudInstallationCommandEntity FromDomain(
        ControlCloudInstallationCommand command)
    {
        var entity = new ControlCloudInstallationCommandEntity();
        UpdateEntity(entity, command);

        return entity;
    }

    private static void UpdateEntity(
        ControlCloudInstallationCommandEntity entity,
        ControlCloudInstallationCommand command)
    {
        entity.CommandId = command.CommandId;
        entity.ClientId = command.ClientId;
        entity.InstallationId = command.InstallationId;
        entity.CommandVersion = command.CommandVersion;
        entity.CommandType = command.CommandType;
        entity.Status = command.Status;
        entity.IdempotencyKey = command.IdempotencyKey;
        entity.PayloadJson = command.PayloadJson;
        entity.SignatureAlgorithm = command.SignatureAlgorithm;
        entity.SignatureKeyId = command.SignatureKeyId;
        entity.PayloadSha256 = command.PayloadSha256;
        entity.SignatureValue = command.SignatureValue;
        entity.QueuedAtUtc = command.QueuedAtUtc;
        entity.NotBeforeUtc = command.NotBeforeUtc;
        entity.ExpiresAtUtc = command.ExpiresAtUtc;
        entity.AcknowledgedAtUtc = command.AcknowledgedAtUtc;
        entity.AcknowledgementStatus = command.AcknowledgementStatus;
        entity.AcknowledgementDetail = command.AcknowledgementDetail;
    }
}
