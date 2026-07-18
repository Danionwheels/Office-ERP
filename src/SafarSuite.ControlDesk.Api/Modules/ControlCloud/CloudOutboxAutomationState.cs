using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.PublishPendingCloudOutboxMessages;

namespace SafarSuite.ControlDesk.Api.Modules.ControlCloud;

public sealed class CloudOutboxAutomationState : ICloudOutboxAutomationState
{
    private readonly object _sync = new();
    private CloudOutboxAutomationSnapshot _snapshot = new(
        false,
        "NotStarted",
        null,
        null,
        null,
        null,
        null,
        0,
        0,
        null);

    public CloudOutboxAutomationSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return _snapshot;
        }
    }

    public void Start(bool enabled, DateTimeOffset startedAtUtc)
    {
        lock (_sync)
        {
            _snapshot = _snapshot with
            {
                Enabled = enabled,
                Status = enabled ? "Idle" : "Disabled",
                StartedAtUtc = startedAtUtc
            };
        }
    }

    public void BeginCycle(DateTimeOffset startedAtUtc)
    {
        lock (_sync)
        {
            _snapshot = _snapshot with
            {
                Status = "Publishing",
                LastCycleStartedAtUtc = startedAtUtc
            };
        }
    }

    public void CompleteCycle(
        PublishPendingCloudOutboxMessagesResult result,
        DateTimeOffset completedAtUtc)
    {
        lock (_sync)
        {
            var publishedAtUtc = result.PublishedCount > 0
                ? completedAtUtc
                : _snapshot.LastPublishSucceededAtUtc;
            var failedAtUtc = result.FailedCount > 0
                ? completedAtUtc
                : _snapshot.LastPublishFailedAtUtc;

            _snapshot = _snapshot with
            {
                Status = result.FailedCount > 0 ? "Backoff" : "Idle",
                LastCycleCompletedAtUtc = completedAtUtc,
                LastPublishSucceededAtUtc = publishedAtUtc,
                LastPublishFailedAtUtc = failedAtUtc,
                LastPublishedCount = result.PublishedCount,
                LastFailedCount = result.FailedCount,
                LastFailureCode = result.FailedCount > 0 ? "CloudPublishFailed" : null
            };
        }
    }

    public void FailCycle(string failureCode, DateTimeOffset failedAtUtc)
    {
        lock (_sync)
        {
            _snapshot = _snapshot with
            {
                Status = "Faulted",
                LastCycleCompletedAtUtc = failedAtUtc,
                LastPublishFailedAtUtc = failedAtUtc,
                LastPublishedCount = 0,
                LastFailedCount = 0,
                LastFailureCode = failureCode
            };
        }
    }

    public void Stop(DateTimeOffset stoppedAtUtc)
    {
        lock (_sync)
        {
            _snapshot = _snapshot with
            {
                Status = _snapshot.Enabled ? "Stopped" : "Disabled",
                LastCycleCompletedAtUtc = _snapshot.LastCycleCompletedAtUtc ?? stoppedAtUtc
            };
        }
    }
}
