namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class ControlCloudPublisherOptions
{
    public const string SectionName = "ControlCloud:Publisher";

    public string Mode { get; set; } = "Local";

    public string SourceSystem { get; set; } = "SafarSuite.ControlDesk";

    public string Environment { get; set; } = "Local";

    public string SigningKeyId { get; set; } = "local-dev";

    public string SigningSecret { get; set; } = string.Empty;

    public string? EndpointUrl { get; set; }

    public bool RequireHttps { get; set; }

    public int MaximumAttemptCount { get; set; } = 5;

    public int RetryDelaySeconds { get; set; } = 60;

    public int RequestTimeoutSeconds { get; set; } = 20;
}
