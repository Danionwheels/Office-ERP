using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.RejectPortalPaymentClaim;

public sealed class RejectPortalPaymentClaimHandler
{
    private readonly IPortalPaymentClaimRepository _claims;
    private readonly ICloudOutboxMessageRepository _cloudOutboxMessages;
    private readonly PaymentCloudOutboxMessageFactory _outboxMessageFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RejectPortalPaymentClaimHandler(
        IPortalPaymentClaimRepository claims,
        ICloudOutboxMessageRepository cloudOutboxMessages,
        PaymentCloudOutboxMessageFactory outboxMessageFactory,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _claims = claims;
        _cloudOutboxMessages = cloudOutboxMessages;
        _outboxMessageFactory = outboxMessageFactory;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<PortalPaymentClaimResult>> HandleAsync(
        RejectPortalPaymentClaimCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClaimId == Guid.Empty)
        {
            return Result<PortalPaymentClaimResult>.Failure(ApplicationError.Validation(
                nameof(command.ClaimId),
                "Claim id cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result<PortalPaymentClaimResult>.Failure(ApplicationError.Validation(
                nameof(command.Reason),
                "Rejection reason is required."));
        }

        try
        {
            var claim = await _claims.GetByIdAsync(
                PortalPaymentClaimId.Create(command.ClaimId),
                cancellationToken);

            if (claim is null)
            {
                return Result<PortalPaymentClaimResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClaimId),
                    "Portal payment claim was not found."));
            }

            if (claim.Status != PortalPaymentClaimStatus.PendingVerification)
            {
                return Result<PortalPaymentClaimResult>.Failure(ApplicationError.Conflict(
                    nameof(command.ClaimId),
                    "Only pending portal payment claims can be rejected."));
            }

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    claim.Reject(command.Reason, _clock.UtcNow);
                    await _cloudOutboxMessages.AddAsync(
                        _outboxMessageFactory.CreatePortalPaymentClaimDecided(
                            claim,
                            paymentId: null,
                            claim.RejectionReason),
                        token);

                    return PortalPaymentClaimResultFactory.From(claim);
                },
                cancellationToken);

            return Result<PortalPaymentClaimResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<PortalPaymentClaimResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<PortalPaymentClaimResult>.Failure(ApplicationError.Conflict(
                nameof(command.ClaimId),
                exception.Message));
        }
    }
}
