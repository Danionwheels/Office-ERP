namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface ICloudOutboxAutomationState
{
    CloudOutboxAutomationSnapshot GetSnapshot();
}

public sealed record CloudOutboxAutomationSnapshot(
    bool Enabled,
    string Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? LastCycleStartedAtUtc,
    DateTimeOffset? LastCycleCompletedAtUtc,
    DateTimeOffset? LastPublishSucceededAtUtc,
    DateTimeOffset? LastPublishFailedAtUtc,
    int LastPublishedCount,
    int LastFailedCount,
    string? LastFailureCode);
