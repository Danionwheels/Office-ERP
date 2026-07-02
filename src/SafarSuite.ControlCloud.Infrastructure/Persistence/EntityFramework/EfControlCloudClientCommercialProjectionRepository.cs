using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class EfControlCloudClientCommercialProjectionRepository
    : IControlCloudClientCommercialProjectionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ControlCloudDbContext _dbContext;

    public EfControlCloudClientCommercialProjectionRepository(ControlCloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ControlCloudClientCommercialProjection?> GetByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ClientCommercialProjections
            .AsNoTracking()
            .SingleOrDefaultAsync(projection => projection.ClientId == clientId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<ControlCloudClientCommercialProjection>(
            entity.ProjectionJson,
            JsonOptions);
    }

    public async Task SaveAsync(
        ControlCloudClientCommercialProjection projection,
        CancellationToken cancellationToken = default)
    {
        var projectionJson = JsonSerializer.Serialize(projection, JsonOptions);
        var entity = await _dbContext.ClientCommercialProjections
            .SingleOrDefaultAsync(stored => stored.ClientId == projection.ClientId, cancellationToken);

        if (entity is null)
        {
            await _dbContext.ClientCommercialProjections.AddAsync(
                new ControlCloudClientCommercialProjectionEntity
                {
                    ClientId = projection.ClientId,
                    LastUpdatedAtUtc = projection.LastUpdatedAtUtc,
                    ProjectionJson = projectionJson
                },
                cancellationToken);

            return;
        }

        entity.LastUpdatedAtUtc = projection.LastUpdatedAtUtc;
        entity.ProjectionJson = projectionJson;
    }
}
