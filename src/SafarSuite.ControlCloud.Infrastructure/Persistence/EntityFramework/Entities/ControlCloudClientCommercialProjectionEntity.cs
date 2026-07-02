namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudClientCommercialProjectionEntity
{
    public Guid ClientId { get; set; }

    public DateTimeOffset LastUpdatedAtUtc { get; set; }

    public string ProjectionJson { get; set; } = "{}";
}
