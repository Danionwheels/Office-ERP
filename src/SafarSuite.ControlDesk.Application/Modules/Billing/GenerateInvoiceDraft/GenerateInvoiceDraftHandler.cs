using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.GenerateInvoiceDraft;

public sealed class GenerateInvoiceDraftHandler
{
    private readonly IInvoiceRepository _invoices;
    private readonly IClientChargeRuleRepository _clientChargeRules;
    private readonly IChargeCodeRepository _chargeCodes;
    private readonly IClientRepository _clients;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly GenerateInvoiceDraftValidator _validator;

    public GenerateInvoiceDraftHandler(
        IInvoiceRepository invoices,
        IClientChargeRuleRepository clientChargeRules,
        IChargeCodeRepository chargeCodes,
        IClientRepository clients,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        GenerateInvoiceDraftValidator validator)
    {
        _invoices = invoices;
        _clientChargeRules = clientChargeRules;
        _chargeCodes = chargeCodes;
        _clients = clients;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<GenerateInvoiceDraftResult>> HandleAsync(
        GenerateInvoiceDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<GenerateInvoiceDraftResult>.Failure(validationErrors);
        }

        try
        {
            var clientId = ClientId.Create(command.ClientId);

            if (await _clients.GetByIdAsync(clientId, cancellationToken) is null)
            {
                return Result<GenerateInvoiceDraftResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClientId),
                    "Client was not found."));
            }

            var contractId = ContractId.Create(command.ContractId);
            var invoiceNumber = InvoiceNumber.Create(command.InvoiceNumber);

            if (await _invoices.ExistsByNumberAsync(invoiceNumber, cancellationToken))
            {
                return Result<GenerateInvoiceDraftResult>.Failure(ApplicationError.Conflict(
                    nameof(command.InvoiceNumber),
                    $"Invoice {invoiceNumber.Value} already exists."));
            }

            var rules = await _clientChargeRules.ListEffectiveForClientAsync(
                clientId,
                contractId,
                command.BillingDate,
                cancellationToken);

            if (rules.Count == 0)
            {
                return Result<GenerateInvoiceDraftResult>.Failure(ApplicationError.Validation(
                    nameof(command.BillingDate),
                    "No active client charge rules were found for this billing date."));
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

            foreach (var rule in rules.OrderBy(rule => rule.ChargeCodeId.Value))
            {
                if (!string.Equals(rule.UnitPrice.CurrencyCode, invoice.CurrencyCode, StringComparison.Ordinal))
                {
                    return Result<GenerateInvoiceDraftResult>.Failure(ApplicationError.Validation(
                        nameof(command.CurrencyCode),
                        "All effective client charge rules must use the invoice currency."));
                }

                var chargeCode = await _chargeCodes.GetByIdAsync(rule.ChargeCodeId, cancellationToken);

                if (chargeCode is null)
                {
                    return Result<GenerateInvoiceDraftResult>.Failure(ApplicationError.NotFound(
                        nameof(rule.ChargeCodeId),
                        "Charge code for an effective client charge rule was not found."));
                }

                var description = rule.DescriptionOverride ?? chargeCode.Name;
                invoice.AddLine(InvoiceLine.Create(description, rule.LineAmount, rule.ChargeCodeId));
            }

            await _invoices.AddAsync(invoice, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<GenerateInvoiceDraftResult>.Success(ToResult(invoice, command.BillingDate));
        }
        catch (ArgumentException exception)
        {
            return Result<GenerateInvoiceDraftResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<GenerateInvoiceDraftResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private static GenerateInvoiceDraftResult ToResult(Invoice invoice, DateOnly billingDate)
    {
        return new GenerateInvoiceDraftResult(
            invoice.Id.Value,
            invoice.ClientId.Value,
            invoice.ContractId.Value,
            invoice.Number.Value,
            invoice.IssueDate,
            invoice.DueDate,
            billingDate,
            invoice.TotalAmount.Amount,
            invoice.BalanceDue.Amount,
            invoice.CurrencyCode,
            invoice.Status.ToString(),
            invoice.Lines.Select(line => new GenerateInvoiceDraftLineResult(
                line.ChargeCodeId?.Value,
                line.Description,
                line.Amount.Amount,
                line.Amount.CurrencyCode)).ToArray());
    }
}
