using System.Security.Cryptography;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.UploadPortalAttachment;

public sealed class UploadClientPortalAttachmentHandler
{
    private readonly IClientPortalAttachmentRepository _attachments;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;
    private readonly ClientPortalAttachmentContentValidator _validator;

    public UploadClientPortalAttachmentHandler(
        IClientPortalAttachmentRepository attachments,
        IClientPortalAuditRecorder audit,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock,
        ClientPortalAttachmentContentValidator validator)
    {
        _attachments = attachments;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<ClientPortalPaymentOperationResult<ControlCloudClientPortalAttachment>> HandleAsync(
        UploadClientPortalAttachmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClientId == Guid.Empty || command.UploadedByUserId == Guid.Empty)
        {
            return Failure("PaymentProofIdentityRequired", "Client and user ids are required.");
        }

        var validation = _validator.Validate(command.FileName, command.ContentType, command.Content);
        if (!validation.IsSuccess)
        {
            return Failure(validation.FailureCode!, validation.Detail!);
        }

        try
        {
            var now = _clock.UtcNow;
            var attachment = ControlCloudClientPortalAttachment.Create(
                Guid.NewGuid(),
                command.ClientId,
                command.UploadedByUserId,
                command.FileName,
                validation.DetectedContentType!,
                command.Content,
                Convert.ToHexString(SHA256.HashData(command.Content)).ToLowerInvariant(),
                now);

            await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    await _attachments.AddAsync(attachment, token);
                    await ControlCloudAuditWriter.TryRecordAsync(
                        _audit,
                        new ClientPortalAuditRecord(
                            Guid.NewGuid(),
                            attachment.ClientId,
                            null,
                            attachment.UploadedByUserId,
                            "",
                            ClientPortalAuditEventTypes.PaymentProofUploaded,
                            ClientPortalAuditActors.ClientPortal,
                            $"Payment proof {attachment.AttachmentId:D} was uploaded.",
                            now),
                        token);
                },
                cancellationToken);

            return ClientPortalPaymentOperationResult<ControlCloudClientPortalAttachment>.Success(attachment);
        }
        catch (ArgumentException exception)
        {
            return Failure("PaymentProofInvalid", exception.Message);
        }
    }

    private static ClientPortalPaymentOperationResult<ControlCloudClientPortalAttachment> Failure(
        string code,
        string detail) =>
        ClientPortalPaymentOperationResult<ControlCloudClientPortalAttachment>.Failure(code, detail);
}
