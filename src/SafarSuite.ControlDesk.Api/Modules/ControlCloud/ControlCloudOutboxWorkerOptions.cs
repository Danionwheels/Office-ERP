namespace SafarSuite.ControlDesk.Api.Modules.ControlCloud;

public sealed class ControlCloudOutboxWorkerOptions
{
    public const string SectionName = "ControlCloud:OutboxWorker";

    public bool Enabled { get; set; }

    public int BatchSize { get; set; } = 20;

    public int PollIntervalSeconds { get; set; } = 15;
}
