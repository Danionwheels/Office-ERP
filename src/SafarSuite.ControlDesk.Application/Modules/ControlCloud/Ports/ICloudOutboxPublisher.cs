using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface ICloudOutboxPublisher
{
    Task<CloudOutboxPublishResult> PublishAsync(
        CloudOutboxMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record CloudOutboxPublishResult(
    bool IsSuccess,
    bool ShouldRetry,
    string? FailureReason,
    string? CloudReference,
    string? EnvelopeSignature)
{
    public static CloudOutboxPublishResult Success(string cloudReference, string envelopeSignature)
    {
        if (string.IsNullOrWhiteSpace(cloudReference))
        {
            throw new ArgumentException("Cloud reference is required.", nameof(cloudReference));
        }

        if (string.IsNullOrWhiteSpace(envelopeSignature))
        {
            throw new ArgumentException("Envelope signature is required.", nameof(envelopeSignature));
        }

        return new CloudOutboxPublishResult(
            true,
            false,
            null,
            cloudReference.Trim(),
            envelopeSignature.Trim());
    }

    public static CloudOutboxPublishResult Failure(string failureReason, bool shouldRetry = true)
    {
        if (string.IsNullOrWhiteSpace(failureReason))
        {
            throw new ArgumentException("Failure reason is required.", nameof(failureReason));
        }

        return new CloudOutboxPublishResult(false, shouldRetry, failureReason.Trim(), null, null);
    }
}
