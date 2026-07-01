using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface ICloudOutboxPublisher
{
    Task<CloudOutboxPublishResult> PublishAsync(
        CloudOutboxMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record CloudOutboxPublishResult(bool IsSuccess, string? FailureReason)
{
    public static CloudOutboxPublishResult Success()
    {
        return new CloudOutboxPublishResult(true, null);
    }

    public static CloudOutboxPublishResult Failure(string failureReason)
    {
        if (string.IsNullOrWhiteSpace(failureReason))
        {
            throw new ArgumentException("Failure reason is required.", nameof(failureReason));
        }

        return new CloudOutboxPublishResult(false, failureReason.Trim());
    }
}
