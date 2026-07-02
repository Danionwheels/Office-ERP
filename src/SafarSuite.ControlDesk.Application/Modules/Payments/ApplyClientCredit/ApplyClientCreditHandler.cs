using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.ApplyClientCredit;

public sealed class ApplyClientCreditHandler
{
    private readonly IClientRepository _clients;
    private readonly IInvoiceRepository _invoices;
    private readonly IClientCreditApplicationRepository _creditApplications;
    private readonly ICloudOutboxMessageRepository _cloudOutboxMessages;
    private readonly ClientCreditBalanceService _creditBalanceService;
    private readonly PaymentCloudOutboxMessageFactory _outboxMessageFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly ApplyClientCreditValidator _validator;

    public ApplyClientCreditHandler(
        IClientRepository clients,
        IInvoiceRepository invoices,
        IClientCreditApplicationRepository creditApplications,
        ICloudOutboxMessageRepository cloudOutboxMessages,
        ClientCreditBalanceService creditBalanceService,
        PaymentCloudOutboxMessageFactory outboxMessageFactory,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        ApplyClientCreditValidator validator)
    {
        _clients = clients;
        _invoices = invoices;
        _creditApplications = creditApplications;
        _cloudOutboxMessages = cloudOutboxMessages;
        _creditBalanceService = creditBalanceService;
        _outboxMessageFactory = outboxMessageFactory;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<ApplyClientCreditResult>> HandleAsync(
        ApplyClientCreditCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<ApplyClientCreditResult>.Failure(validationErrors);
        }

        try
        {
            var clientId = ClientId.Create(command.ClientId);
            var client = await _clients.GetByIdAsync(clientId, cancellationToken);

            if (client is null)
            {
                return Result<ApplyClientCreditResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClientId),
                    "Client was not found."));
            }

            var invoice = await _invoices.GetByIdAsync(InvoiceId.Create(command.InvoiceId), cancellationToken);

            if (invoice is null)
            {
                return Result<ApplyClientCreditResult>.Failure(ApplicationError.NotFound(
                    nameof(command.InvoiceId),
                    "Invoice was not found."));
            }

            if (invoice.ClientId != clientId)
            {
                return Result<ApplyClientCreditResult>.Failure(ApplicationError.Validation(
                    nameof(command.InvoiceId),
                    "Invoice does not belong to the selected client."));
            }

            if (invoice.Status is not (InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid))
            {
                return Result<ApplyClientCreditResult>.Failure(ApplicationError.Validation(
                    nameof(command.InvoiceId),
                    "Credit can only be applied to issued or partially paid invoices."));
            }

            var reference = ClientCreditApplicationReference.Create(command.Reference);

            if (await _creditApplications.ExistsByReferenceAsync(reference, cancellationToken))
            {
                return Result<ApplyClientCreditResult>.Failure(ApplicationError.Conflict(
                    nameof(command.Reference),
                    $"Credit application {reference.Value} already exists."));
            }

            var appliedAmount = Money.Of(command.Amount, command.CurrencyCode);

            if (!string.Equals(invoice.CurrencyCode, appliedAmount.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return Result<ApplyClientCreditResult>.Failure(ApplicationError.Validation(
                    nameof(command.CurrencyCode),
                    "Applied credit currency must match the invoice currency."));
            }

            return await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var creditBalance = await _creditBalanceService.CalculateAsync(
                        clientId,
                        appliedAmount.CurrencyCode,
                        token);
                    var invoiceBalanceBefore = invoice.BalanceDue.Amount;

                    if (appliedAmount.Amount > creditBalance.AvailableCredit)
                    {
                        return Result<ApplyClientCreditResult>.Failure(ApplicationError.Validation(
                            nameof(command.Amount),
                            $"Applied credit exceeds available client credit of {creditBalance.AvailableCredit:0.00} {appliedAmount.CurrencyCode}."));
                    }

                    if (appliedAmount.Amount > invoiceBalanceBefore)
                    {
                        return Result<ApplyClientCreditResult>.Failure(ApplicationError.Validation(
                            nameof(command.Amount),
                            $"Applied credit exceeds invoice balance due of {invoiceBalanceBefore:0.00} {appliedAmount.CurrencyCode}."));
                    }

                    var application = ClientCreditApplication.Apply(
                        ClientCreditApplicationId.Create(_idGenerator.NewGuid()),
                        clientId,
                        invoice.Id,
                        reference,
                        appliedAmount,
                        command.AppliedOn,
                        command.Note,
                        _clock.UtcNow);

                    invoice.ApplyCredit(appliedAmount);

                    var invoiceBalanceAfter = invoice.BalanceDue.Amount;
                    var availableCreditAfter = creditBalance.AvailableCredit - appliedAmount.Amount;

                    await _creditApplications.AddAsync(application, token);
                    await _cloudOutboxMessages.AddAsync(
                        _outboxMessageFactory.CreateClientCreditApplied(
                            application,
                            invoice,
                            invoiceBalanceBefore,
                            invoiceBalanceAfter,
                            creditBalance.AvailableCredit,
                            availableCreditAfter,
                            creditBalance.StatementBalance,
                            creditBalance.StatementBalance),
                        token);

                    return Result<ApplyClientCreditResult>.Success(ToResult(
                        application,
                        invoice,
                        invoiceBalanceBefore,
                        invoiceBalanceAfter,
                        creditBalance.AvailableCredit,
                        availableCreditAfter,
                        creditBalance.StatementBalance,
                        creditBalance.StatementBalance));
                },
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Result<ApplyClientCreditResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<ApplyClientCreditResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private static ApplyClientCreditResult ToResult(
        ClientCreditApplication application,
        Invoice invoice,
        decimal invoiceBalanceBefore,
        decimal invoiceBalanceAfter,
        decimal availableCreditBefore,
        decimal availableCreditAfter,
        decimal clientBalanceBefore,
        decimal clientBalanceAfter)
    {
        return new ApplyClientCreditResult(
            application.Id.Value,
            application.ClientId.Value,
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.Status.ToString(),
            application.Reference.Value,
            application.Amount.Amount,
            invoiceBalanceBefore,
            invoiceBalanceAfter,
            availableCreditBefore,
            availableCreditAfter,
            clientBalanceBefore,
            clientBalanceAfter,
            application.CurrencyCode,
            application.AppliedOn,
            application.Status.ToString());
    }
}
