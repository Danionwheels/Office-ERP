namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudInstallationCommandAcknowledgementEntity
{
    public Guid AcknowledgementId { get; set; }

    public Guid CommandId { get; set; }

    public Guid ClientId { get; set; }

    public string InstallationId { get; set; } = "";

    public long CommandVersion { get; set; }

    public string ResultStatus { get; set; } = "";

    public string? Detail { get; set; }

    public string PayloadJson { get; set; } = "{}";

    public DateTimeOffset AcknowledgedAtUtc { get; set; }
}
