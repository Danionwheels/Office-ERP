using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.Ports;
using SafarSuite.ControlDesk.Application.Modules.SurveyValuation.SurveyJobEntry;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.CreateSurveyJobBillingDraft;

public sealed class CreateSurveyJobBillingDraftHandler
{
    private readonly ISurveyJobRepository _surveyJobs;
    private readonly IClientRepository _clients;
    private readonly IInvoiceRepository _invoices;
    private readonly IChargeCodeRepository _chargeCodes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly CreateSurveyJobBillingDraftValidator _validator;

    public CreateSurveyJobBillingDraftHandler(
        ISurveyJobRepository surveyJobs,
        IClientRepository clients,
        IInvoiceRepository invoices,
        IChargeCodeRepository chargeCodes,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        CreateSurveyJobBillingDraftValidator validator)
    {
        _surveyJobs = surveyJobs;
        _clients = clients;
        _invoices = invoices;
        _chargeCodes = chargeCodes;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<CreateSurveyJobBillingDraftResult>> HandleAsync(
        CreateSurveyJobBillingDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<CreateSurveyJobBillingDraftResult>.Failure(validationErrors);
        }

        try
        {
            var surveyJob = await _surveyJobs.GetByIdAsync(
                SurveyJobId.Create(command.SurveyJobId),
                cancellationToken);

            if (surveyJob is null)
            {
                return Result<CreateSurveyJobBillingDraftResult>.Failure(ApplicationError.NotFound(
                    nameof(command.SurveyJobId),
                    "Survey job was not found."));
            }

            if (surveyJob.InvoiceLines.Count == 0)
            {
                return Result<CreateSurveyJobBillingDraftResult>.Failure(ApplicationError.Validation(
                    nameof(surveyJob.InvoiceLines),
                    "Survey job must have prepared invoice lines before creating a billing draft."));
            }

            var clientId = ClientId.Create(command.ClientId);

            if (await _clients.GetByIdAsync(clientId, cancellationToken) is null)
            {
                return Result<CreateSurveyJobBillingDraftResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClientId),
                    "Client was not found."));
            }

            var contractId = ContractId.Create(command.ContractId);
            var invoiceNumber = InvoiceNumber.Create(command.InvoiceNumber);

            if (await _invoices.ExistsByNumberAsync(invoiceNumber, cancellationToken))
            {
                return Result<CreateSurveyJobBillingDraftResult>.Failure(ApplicationError.Conflict(
                    nameof(command.InvoiceNumber),
                    $"Invoice {invoiceNumber.Value} already exists."));
            }

            var mappedLines = await MapPreparedLinesAsync(surveyJob, command.CurrencyCode, cancellationToken);

            if (mappedLines.IsFailure)
            {
                return Result<CreateSurveyJobBillingDraftResult>.Failure(mappedLines.Errors);
            }

            var invoice = Invoice.Create(
                InvoiceId.Create(_idGenerator.NewGuid()),
                clientId,
                contractId,
                invoiceNumber,
                command.IssueDate,
                command.DueDate,
                command.CurrencyCode,
                _clock.UtcNow);

            foreach (var line in mappedLines.Value)
            {
                invoice.AddLine(InvoiceLine.Create(line.Description, line.Amount, line.ChargeCode.Id));
            }

            if (invoice.TotalAmount.Amount <= 0)
            {
                return Result<CreateSurveyJobBillingDraftResult>.Failure(ApplicationError.Validation(
                    nameof(surveyJob.InvoiceLines),
                    "Survey job prepared invoice lines must total more than zero before creating a billing draft."));
            }

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    await _invoices.AddAsync(invoice, token);
                    surveyJob.UpdateInvoiceSummary(CreateUpdatedInvoiceSummary(surveyJob, invoice));

                    return ToResult(surveyJob, invoice, mappedLines.Value);
                },
                cancellationToken);

            return Result<CreateSurveyJobBillingDraftResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<CreateSurveyJobBillingDraftResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CreateSurveyJobBillingDraftResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private async Task<Result<IReadOnlyCollection<MappedSurveyInvoiceLine>>> MapPreparedLinesAsync(
        SurveyJob surveyJob,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        var mappedLines = new List<MappedSurveyInvoiceLine>();

        foreach (var line in surveyJob.InvoiceLines.OrderBy(line => line.SequenceNumber))
        {
            if (!string.Equals(line.Amount.CurrencyCode, currencyCode.Trim().ToUpperInvariant(), StringComparison.Ordinal))
            {
                return Result<IReadOnlyCollection<MappedSurveyInvoiceLine>>.Failure(ApplicationError.Validation(
                    nameof(currencyCode),
                    "Every prepared survey invoice line must match the billing draft currency."));
            }

            if (line.BillingHeadCode is null)
            {
                return Result<IReadOnlyCollection<MappedSurveyInvoiceLine>>.Failure(ApplicationError.Validation(
                    nameof(line.BillingHeadCode),
                    "Every prepared survey invoice line must include a billing head code before creating a billing draft."));
            }

            var chargeCode = await _chargeCodes.GetByCodeAsync(
                ChargeCodeKey.Create(line.BillingHeadCode.Value),
                cancellationToken);

            if (chargeCode is null)
            {
                return Result<IReadOnlyCollection<MappedSurveyInvoiceLine>>.Failure(ApplicationError.NotFound(
                    nameof(line.BillingHeadCode),
                    $"Charge code {line.BillingHeadCode.Value} was not found for a prepared survey invoice line."));
            }

            if (chargeCode.Status != ChargeCodeStatus.Active)
            {
                return Result<IReadOnlyCollection<MappedSurveyInvoiceLine>>.Failure(ApplicationError.Validation(
                    nameof(line.BillingHeadCode),
                    $"Charge code {line.BillingHeadCode.Value} is not active."));
            }

            mappedLines.Add(new MappedSurveyInvoiceLine(line.Description, line.Amount, chargeCode));
        }

        return Result<IReadOnlyCollection<MappedSurveyInvoiceLine>>.Success(mappedLines);
    }

    private static SurveyJobInvoiceSummary CreateUpdatedInvoiceSummary(SurveyJob surveyJob, Invoice invoice)
    {
        return SurveyJobInvoiceSummary.Create(
            invoice.Number.Value,
            invoice.TotalAmount,
            invoice.TotalAmount,
            surveyJob.InvoiceSummary.DiscountAmount,
            surveyJob.InvoiceSummary.SalesTaxPercent,
            surveyJob.InvoiceSummary.SalesTaxAmount,
            surveyJob.InvoiceSummary.PaymentMode,
            surveyJob.InvoiceSummary.WorkshopPaymentAmount,
            surveyJob.InvoiceSummary.VoucherNumber,
            surveyJob.InvoiceSummary.JournalCode,
            surveyJob.InvoiceSummary.DiscountJournalCode);
    }

    private static CreateSurveyJobBillingDraftResult ToResult(
        SurveyJob surveyJob,
        Invoice invoice,
        IReadOnlyCollection<MappedSurveyInvoiceLine> mappedLines)
    {
        return new CreateSurveyJobBillingDraftResult(
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.Status.ToString(),
            invoice.TotalAmount.Amount,
            invoice.BalanceDue.Amount,
            invoice.CurrencyCode,
            mappedLines.Select(line => new CreateSurveyJobBillingDraftLineResult(
                line.ChargeCode.Id.Value,
                line.ChargeCode.Code.Value,
                line.Description,
                line.Amount.Amount,
                line.Amount.CurrencyCode)).ToArray(),
            SurveyJobEntryMapper.ToDto(surveyJob));
    }

    private sealed record MappedSurveyInvoiceLine(
        string Description,
        Domain.SharedKernel.Money Amount,
        ChargeCode ChargeCode);
}
