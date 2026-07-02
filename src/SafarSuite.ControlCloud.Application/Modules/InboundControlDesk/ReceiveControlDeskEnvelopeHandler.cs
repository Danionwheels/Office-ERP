using System.Text.Json;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.InboundControlDesk.Ports;
using SafarSuite.ControlCloud.Domain.Modules.InboundControlDesk;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.InboundControlDesk;

public sealed class ReceiveControlDeskEnvelopeHandler
{
    private readonly IControlDeskEnvelopeReceiptRepository _receipts;
    private readonly ControlCloudEnvelopeSignatureValidator _signatureValidator;
    private readonly ControlDeskEnvelopeProjectionService _projectionService;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public ReceiveControlDeskEnvelopeHandler(
        IControlDeskEnvelopeReceiptRepository receipts,
        ControlCloudEnvelopeSignatureValidator signatureValidator,
        ControlDeskEnvelopeProjectionService projectionService,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _receipts = receipts;
        _signatureValidator = signatureValidator;
        _projectionService = projectionService;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ReceiveControlDeskEnvelopeResult> HandleAsync(
        ReceiveControlDeskEnvelopeCommand command,
        CancellationToken cancellationToken = default)
    {
        var envelope = command.Envelope;
        var validation = _signatureValidator.Validate(envelope);
        var receivedAtUtc = _clock.UtcNow;

        if (!validation.IsValid)
        {
            var rejectedReceiptId = Guid.NewGuid();
            var rejectedReceipt = ControlDeskEnvelopeReceipt.Rejected(
                rejectedReceiptId,
                envelope.MessageId,
                envelope.MessageType ?? "",
                envelope.SubjectType ?? "",
                envelope.SubjectId ?? "",
                envelope.SourceSystem ?? "",
                envelope.SourceEnvironment ?? "",
                envelope.IdempotencyKey ?? "",
                envelope.Signature?.KeyId ?? "",
                envelope.Signature?.Value ?? "",
                $"rejected-{rejectedReceiptId:N}",
                envelope.OccurredAtUtc,
                envelope.PreparedAtUtc,
                receivedAtUtc,
                validation.Detail ?? "Envelope rejected.");

            return await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    await _receipts.AddAsync(rejectedReceipt, token);

                    return ToResult(
                        rejectedReceipt,
                        validation.Code);
                },
                cancellationToken);
        }

        var existingReceipt = await _receipts.GetAcceptedByIdempotencyKeyAsync(
            envelope.IdempotencyKey,
            cancellationToken);

        if (existingReceipt is not null)
        {
            var duplicateReceipt = ControlDeskEnvelopeReceipt.Duplicate(
                Guid.NewGuid(),
                envelope.MessageId,
                envelope.MessageType,
                envelope.SubjectType,
                envelope.SubjectId,
                envelope.SourceSystem,
                envelope.SourceEnvironment,
                envelope.IdempotencyKey,
                envelope.Signature.KeyId,
                envelope.Signature.Value,
                existingReceipt.CloudReference,
                envelope.OccurredAtUtc,
                envelope.PreparedAtUtc,
                receivedAtUtc);

            return await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    await _receipts.AddAsync(duplicateReceipt, token);

                    return ToResult(duplicateReceipt);
                },
                cancellationToken);
        }

        try
        {
            var acceptedReceipt = ControlDeskEnvelopeReceipt.Accepted(
                Guid.NewGuid(),
                envelope.MessageId,
                envelope.MessageType,
                envelope.SubjectType,
                envelope.SubjectId,
                envelope.SourceSystem,
                envelope.SourceEnvironment,
                envelope.IdempotencyKey,
                envelope.Signature.KeyId,
                envelope.Signature.Value,
                CreateCloudReference(envelope),
                envelope.OccurredAtUtc,
                envelope.PreparedAtUtc,
                receivedAtUtc);

            return await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    await _projectionService.ProjectAsync(envelope, receivedAtUtc, token);
                    await _receipts.AddAsync(acceptedReceipt, token);

                    return ToResult(acceptedReceipt);
                },
                cancellationToken);
        }
        catch (Exception exception) when (IsProjectionFailure(exception))
        {
            var rejectedReceiptId = Guid.NewGuid();
            var rejectedReceipt = ControlDeskEnvelopeReceipt.Rejected(
                rejectedReceiptId,
                envelope.MessageId,
                envelope.MessageType,
                envelope.SubjectType,
                envelope.SubjectId,
                envelope.SourceSystem,
                envelope.SourceEnvironment,
                envelope.IdempotencyKey,
                envelope.Signature.KeyId,
                envelope.Signature.Value,
                $"rejected-{rejectedReceiptId:N}",
                envelope.OccurredAtUtc,
                envelope.PreparedAtUtc,
                receivedAtUtc,
                $"Envelope projection failed: {exception.Message}");

            return await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    await _receipts.AddAsync(rejectedReceipt, token);

                    return ToResult(
                        rejectedReceipt,
                        "ProjectionFailed");
                },
                cancellationToken);
        }
    }

    private static ReceiveControlDeskEnvelopeResult ToResult(
        ControlDeskEnvelopeReceipt receipt,
        string? rejectionCode = null)
    {
        return new ReceiveControlDeskEnvelopeResult(
            receipt.ReceiptId,
            receipt.MessageId,
            receipt.MessageType,
            receipt.SubjectType,
            receipt.SubjectId,
            receipt.IdempotencyKey,
            receipt.CloudReference,
            receipt.Status,
            receipt.Detail,
            rejectionCode);
    }

    private static string CreateCloudReference(ControlCloudEnvelope envelope)
    {
        return $"cc-{envelope.MessageId:N}";
    }

    private static bool IsProjectionFailure(Exception exception)
    {
        return exception is KeyNotFoundException
            or FormatException
            or InvalidOperationException
            or JsonException;
    }
}
