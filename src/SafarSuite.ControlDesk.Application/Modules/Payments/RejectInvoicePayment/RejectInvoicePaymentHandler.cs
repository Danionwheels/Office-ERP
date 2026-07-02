using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.RejectInvoicePayment;

public sealed class RejectInvoicePaymentHandler
{
    private readonly IPaymentRepository _payments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RejectInvoicePaymentValidator _validator;

    public RejectInvoicePaymentHandler(
        IPaymentRepository payments,
        IUnitOfWork unitOfWork,
        RejectInvoicePaymentValidator validator)
    {
        _payments = payments;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<RejectInvoicePaymentResult>> HandleAsync(
        RejectInvoicePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<RejectInvoicePaymentResult>.Failure(validationErrors);
        }

        try
        {
            var payment = await _payments.GetByIdAsync(PaymentId.Create(command.PaymentId), cancellationToken);

            if (payment is null)
            {
                return Result<RejectInvoicePaymentResult>.Failure(ApplicationError.NotFound(
                    nameof(command.PaymentId),
                    "Payment was not found."));
            }

            payment.Reject(command.DecisionNote);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<RejectInvoicePaymentResult>.Success(new RejectInvoicePaymentResult(
                payment.Id.Value,
                payment.InvoiceId.Value,
                payment.Status.ToString(),
                payment.DecisionNote));
        }
        catch (ArgumentException exception)
        {
            return Result<RejectInvoicePaymentResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<RejectInvoicePaymentResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }
}
