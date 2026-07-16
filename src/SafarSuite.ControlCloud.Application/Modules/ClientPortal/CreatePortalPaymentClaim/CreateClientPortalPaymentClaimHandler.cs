using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.CreatePortalPaymentClaim;

public sealed class CreateClientPortalPaymentClaimHandler
{
    private readonly IControlCloudClientCommercialProjectionRepository _projections;
    private readonly IClientPortalPaymentClaimRepository _claims;
    private readonly IClientPortalAttachmentRepository _attachments;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public CreateClientPortalPaymentClaimHandler(
        IControlCloudClientCommercialProjectionRepository projections,
        IClientPortalPaymentClaimRepository claims,
        IClientPortalAttachmentRepository attachments,
        IClientPortalAuditRecorder audit,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _projections = projections;
        _claims = claims;
        _attachments = attachments;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ClientPortalPaymentOperationResult<ClientPortalPaymentClaimView>> HandleAsync(
        CreateClientPortalPaymentClaimCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClientId == Guid.Empty || command.SubmittedByUserId == Guid.Empty
            || command.InvoiceId == Guid.Empty)
        {
            return Failure("PaymentClaimIdentityRequired", "Client, user, and invoice ids are required.");
        }

        var invoice = await _projections.GetInvoiceAsync(
            command.ClientId,
            command.InvoiceId,
            cancellationToken);
        if (invoice is null)
        {
            return Failure("PortalInvoiceNotFound", "Invoice was not found for this client.");
        }

        if (!invoice.InvoiceStatus.Equals("Issued", StringComparison.OrdinalIgnoreCase)
            && !invoice.InvoiceStatus.Equals("PartiallyPaid", StringComparison.OrdinalIgnoreCase))
        {
            return Failure("PortalInvoiceNotPayable", "Only issued or partially paid invoices can receive a payment claim.");
        }

        if (command.Amount <= 0 || command.Amount > invoice.BalanceDue)
        {
            return Failure("PaymentClaimAmountInvalid", "Claim amount must be positive and cannot exceed the invoice balance.");
        }

        ControlCloudClientPortalAttachment? attachment = null;
        if (command.ProofAttachmentId is not null)
        {
            attachment = await _attachments.GetByIdAsync(command.ProofAttachmentId.Value, cancellationToken);
            if (attachment is null
                || attachment.ClientId != command.ClientId
                || attachment.UploadedByUserId != command.SubmittedByUserId)
            {
                return Failure("PaymentProofNotFound", "Payment proof was not found for this portal user.");
            }
        }

        string normalizedReference;
        try
        {
            normalizedReference = ControlCloudClientPortalPaymentClaim.NormalizeReference(
                command.TransferReferenceNumber);
        }
        catch (ArgumentException exception)
        {
            return Failure("PaymentClaimReferenceInvalid", exception.Message);
        }

        if (await _claims.GetByClientAndReferenceAsync(
                command.ClientId,
                normalizedReference,
                cancellationToken) is not null)
        {
            return Failure("PaymentClaimDuplicate", "A payment claim already uses this transfer reference.");
        }

        var pendingAmount = (await _claims.ListAsync(command.ClientId, cancellationToken))
            .Where(claim => claim.InvoiceId == command.InvoiceId
                && claim.Status == ControlCloudClientPortalPaymentClaimStatus.PendingVerification)
            .Sum(claim => claim.Amount);
        var availableBalance = Math.Max(invoice.BalanceDue - pendingAmount, 0);

        if (command.Amount > availableBalance)
        {
            return Failure(
                "PaymentClaimAmountInvalid",
                "Claim amount exceeds the invoice balance available after pending claims.");
        }

        try
        {
            var now = _clock.UtcNow;
            var claim = ControlCloudClientPortalPaymentClaim.Create(
                Guid.NewGuid(),
                command.ClientId,
                command.SubmittedByUserId,
                command.InvoiceId,
                invoice.InvoiceNumber,
                command.Amount,
                invoice.CurrencyCode,
                command.TransferReferenceNumber,
                command.ProofAttachmentId,
                now);

            return await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    await _claims.AddAsync(claim, token);
                    await ControlCloudAuditWriter.TryRecordAsync(
                        _audit,
                        new ClientPortalAuditRecord(
                            Guid.NewGuid(),
                            claim.ClientId,
                            null,
                            claim.SubmittedByUserId,
                            "",
                            ClientPortalAuditEventTypes.PaymentClaimSubmitted,
                            ClientPortalAuditActors.ClientPortal,
                            $"Payment claim {claim.ClaimId:D} was submitted for invoice {claim.InvoiceId:D}.",
                            now),
                        token);
                    return ClientPortalPaymentOperationResult<ClientPortalPaymentClaimView>.Success(
                        new ClientPortalPaymentClaimView(claim, attachment));
                },
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Failure("PaymentClaimInvalid", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure("PaymentClaimConflict", exception.Message);
        }
    }

    private static ClientPortalPaymentOperationResult<ClientPortalPaymentClaimView> Failure(
        string code,
        string detail) =>
        ClientPortalPaymentOperationResult<ClientPortalPaymentClaimView>.Failure(code, detail);
}
