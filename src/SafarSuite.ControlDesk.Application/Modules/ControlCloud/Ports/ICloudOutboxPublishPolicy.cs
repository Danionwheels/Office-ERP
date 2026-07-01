namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface ICloudOutboxPublishPolicy
{
    int MaximumAttemptCount { get; }

    TimeSpan RetryDelay { get; }
}
