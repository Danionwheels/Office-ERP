using System.Text.Json;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;

public sealed class IssueInvoiceHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IInvoiceRepository _invoices;
    private readonly IChargeCodeRepository _chargeCodes;
    private readonly IClientAccountingProfileRepository _clientAccountingProfiles;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly ICloudOutboxMessageRepository _cloudOutboxMessages;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly IssueInvoiceValidator _validator;

    public IssueInvoiceHandler(
        IInvoiceRepository invoices,
        IChargeCodeRepository chargeCodes,
        IClientAccountingProfileRepository clientAccountingProfiles,
        ILedgerAccountRepository ledgerAccounts,
        IJournalEntryRepository journalEntries,
        ICloudOutboxMessageRepository cloudOutboxMessages,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        IssueInvoiceValidator validator)
    {
        _invoices = invoices;
        _chargeCodes = chargeCodes;
        _clientAccountingProfiles = clientAccountingProfiles;
        _ledgerAccounts = ledgerAccounts;
        _journalEntries = journalEntries;
        _cloudOutboxMessages = cloudOutboxMessages;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<IssueInvoiceResult>> HandleAsync(
        IssueInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<IssueInvoiceResult>.Failure(validationErrors);
        }

        try
        {
            var invoiceId = InvoiceId.Create(command.InvoiceId);

            var invoice = await _invoices.GetByIdAsync(invoiceId, cancellationToken);

            if (invoice is null)
            {
                return Result<IssueInvoiceResult>.Failure(ApplicationError.NotFound(
                    nameof(command.InvoiceId),
                    "Invoice was not found."));
            }

            if (invoice.Status != InvoiceStatus.Draft)
            {
                return Result<IssueInvoiceResult>.Failure(ApplicationError.Validation(
                    nameof(command.InvoiceId),
                "Only draft invoices can be issued."));
            }

            var receivableAccountIdResult = await ResolveReceivableAccountIdAsync(
                command,
                invoice,
                cancellationToken);

            if (receivableAccountIdResult.IsFailure)
            {
                return Result<IssueInvoiceResult>.Failure(receivableAccountIdResult.Errors);
            }

            var receivableAccountId = receivableAccountIdResult.Value;
            var receivableAccount = await _ledgerAccounts.GetByIdAsync(receivableAccountId, cancellationToken);

            if (receivableAccount is null)
            {
                return Result<IssueInvoiceResult>.Failure(ApplicationError.NotFound(
                    nameof(command.AccountsReceivableAccountId),
                    "Accounts receivable ledger account was not found."));
            }

            var receivableValidation = ValidateReceivableAccount(command, receivableAccount);

            if (receivableValidation.Count > 0)
            {
                return Result<IssueInvoiceResult>.Failure(receivableValidation);
            }

            var revenueLines = await BuildRevenueLinesAsync(invoice, cancellationToken);

            if (revenueLines.IsFailure)
            {
                return Result<IssueInvoiceResult>.Failure(revenueLines.Errors);
            }

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var journalEntry = CreateJournalEntry(invoice, receivableAccountId, revenueLines.Value, command.PostingDate);

                    invoice.Issue();
                    journalEntry.Post(_clock.UtcNow);

                    await _journalEntries.AddAsync(journalEntry, token);
                    await _cloudOutboxMessages.AddAsync(
                        CreateInvoiceIssuedOutboxMessage(invoice, journalEntry, receivableAccountId),
                        token);

                    return ToResult(invoice, journalEntry);
                },
                cancellationToken);

            return Result<IssueInvoiceResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<IssueInvoiceResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<IssueInvoiceResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private async Task<Result<LedgerAccountId>> ResolveReceivableAccountIdAsync(
        IssueInvoiceCommand command,
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        if (command.AccountsReceivableAccountId.HasValue)
        {
            return Result<LedgerAccountId>.Success(LedgerAccountId.Create(command.AccountsReceivableAccountId.Value));
        }

        var profile = await _clientAccountingProfiles.GetByClientIdAsync(invoice.ClientId, cancellationToken);

        if (profile is null)
        {
            return Result<LedgerAccountId>.Failure(ApplicationError.Validation(
                nameof(command.AccountsReceivableAccountId),
                "Client accounting profile is required before issuing invoice."));
        }

        if (!string.Equals(invoice.CurrencyCode, profile.DefaultCurrencyCode, StringComparison.Ordinal))
        {
            return Result<LedgerAccountId>.Failure(ApplicationError.Validation(
                nameof(invoice.CurrencyCode),
                $"Invoice currency {invoice.CurrencyCode} does not match client profile currency {profile.DefaultCurrencyCode}."));
        }

        return Result<LedgerAccountId>.Success(profile.AccountsReceivableAccountId);
    }

    private static IReadOnlyCollection<ApplicationError> ValidateReceivableAccount(
        IssueInvoiceCommand command,
        LedgerAccount receivableAccount)
    {
        var errors = new List<ApplicationError>();

        if (receivableAccount.Type != LedgerAccountType.Asset)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.AccountsReceivableAccountId),
                "Accounts receivable ledger account must be an asset account."));
        }

        if (!receivableAccount.IsPostingAccount)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.AccountsReceivableAccountId),
                "Accounts receivable ledger account must be a posting account."));
        }

        if (receivableAccount.Status != LedgerAccountStatus.Active)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.AccountsReceivableAccountId),
                "Accounts receivable ledger account must be active."));
        }

        return errors;
    }

    private async Task<Result<IReadOnlyCollection<RevenuePostingLine>>> BuildRevenueLinesAsync(
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        var revenueLines = new List<RevenuePostingLine>();

        foreach (var invoiceLine in invoice.Lines)
        {
            if (!invoiceLine.ChargeCodeId.HasValue)
            {
                return Result<IReadOnlyCollection<RevenuePostingLine>>.Failure(ApplicationError.Validation(
                    nameof(invoiceLine.ChargeCodeId),
                    "Every invoice line must reference a charge code before posting."));
            }

            var chargeCode = await _chargeCodes.GetByIdAsync(invoiceLine.ChargeCodeId.Value, cancellationToken);

            if (chargeCode is null)
            {
                return Result<IReadOnlyCollection<RevenuePostingLine>>.Failure(ApplicationError.NotFound(
                    nameof(invoiceLine.ChargeCodeId),
                    "Charge code for an invoice line was not found."));
            }

            revenueLines.Add(new RevenuePostingLine(
                chargeCode.RevenueAccountId,
                invoiceLine.Amount,
                invoiceLine.Description));
        }

        return Result<IReadOnlyCollection<RevenuePostingLine>>.Success(revenueLines);
    }

    private JournalEntry CreateJournalEntry(
        Invoice invoice,
        LedgerAccountId receivableAccountId,
        IReadOnlyCollection<RevenuePostingLine> revenueLines,
        DateOnly postingDate)
    {
        var journalEntry = JournalEntry.Create(
            JournalEntryId.Create(_idGenerator.NewGuid()),
            postingDate,
            invoice.CurrencyCode,
            JournalSourceType.BillingInvoice,
            invoice.Number.Value,
            $"Invoice {invoice.Number.Value}",
            _clock.UtcNow);

        journalEntry.AddLine(JournalLine.DebitLine(
            receivableAccountId,
            invoice.TotalAmount,
            $"Accounts receivable for invoice {invoice.Number.Value}"));

        foreach (var group in revenueLines.GroupBy(line => line.RevenueAccountId))
        {
            var amount = group
                .Select(line => line.Amount)
                .Aggregate((total, amount) => total.Add(amount));

            var description = group.Count() == 1
                ? group.First().Description
                : $"Revenue for invoice {invoice.Number.Value}";

            journalEntry.AddLine(JournalLine.CreditLine(group.Key, amount, description));
        }

        return journalEntry;
    }

    private CloudOutboxMessage CreateInvoiceIssuedOutboxMessage(
        Invoice invoice,
        JournalEntry journalEntry,
        LedgerAccountId receivableAccountId)
    {
        var payload = new InvoiceIssuedCloudPayload(
            "1",
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.ClientId.Value,
            invoice.ContractId.Value,
            invoice.Status.ToString(),
            invoice.IssueDate,
            invoice.DueDate,
            invoice.TotalAmount.Amount,
            invoice.BalanceDue.Amount,
            invoice.CurrencyCode,
            receivableAccountId.Value,
            journalEntry.Id.Value,
            journalEntry.EntryDate,
            journalEntry.Status.ToString(),
            invoice.Lines.Select(line => new InvoiceIssuedCloudPayloadLine(
                line.ChargeCodeId?.Value,
                line.Description,
                line.Amount.Amount,
                line.Amount.CurrencyCode)).ToArray());

        return CloudOutboxMessage.Create(
            CloudOutboxMessageId.Create(_idGenerator.NewGuid()),
            "InvoiceIssued",
            "Invoice",
            invoice.Id.Value.ToString(),
            JsonSerializer.Serialize(payload, JsonOptions),
            _clock.UtcNow);
    }

    private static IssueInvoiceResult ToResult(Invoice invoice, JournalEntry journalEntry)
    {
        return new IssueInvoiceResult(
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.Status.ToString(),
            journalEntry.Id.Value,
            journalEntry.Status.ToString(),
            journalEntry.EntryDate,
            journalEntry.TotalDebit.Amount,
            journalEntry.TotalCredit.Amount,
            journalEntry.CurrencyCode,
            journalEntry.Lines.Select(line => new IssueInvoiceJournalLineResult(
                line.LedgerAccountId.Value,
                line.Debit.Amount,
                line.Credit.Amount,
                line.Description)).ToArray());
    }

    private sealed record RevenuePostingLine(
        LedgerAccountId RevenueAccountId,
        Money Amount,
        string Description);

    private sealed record InvoiceIssuedCloudPayload(
        string EventVersion,
        Guid InvoiceId,
        string InvoiceNumber,
        Guid ClientId,
        Guid ContractId,
        string InvoiceStatus,
        DateOnly IssueDate,
        DateOnly DueDate,
        decimal TotalAmount,
        decimal BalanceDue,
        string CurrencyCode,
        Guid AccountsReceivableAccountId,
        Guid JournalEntryId,
        DateOnly PostingDate,
        string JournalEntryStatus,
        IReadOnlyCollection<InvoiceIssuedCloudPayloadLine> Lines);

    private sealed record InvoiceIssuedCloudPayloadLine(
        Guid? ChargeCodeId,
        string Description,
        decimal Amount,
        string CurrencyCode);
}
