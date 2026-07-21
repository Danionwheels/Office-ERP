using System.Text.Json;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Common;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;

public sealed class IssueInvoiceHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IInvoiceRepository _invoices;
    private readonly IClientRepository _clients;
    private readonly IChargeCodeRepository _chargeCodes;
    private readonly IClientAccountingProfileRepository _clientAccountingProfiles;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly AccountingPeriodPostingGuard _periodGuard;
    private readonly ICloudOutboxMessageRepository _cloudOutboxMessages;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly IssueInvoiceValidator _validator;

    public IssueInvoiceHandler(
        IInvoiceRepository invoices,
        IClientRepository clients,
        IChargeCodeRepository chargeCodes,
        IClientAccountingProfileRepository clientAccountingProfiles,
        ILedgerAccountRepository ledgerAccounts,
        IJournalEntryRepository journalEntries,
        AccountingPeriodPostingGuard periodGuard,
        ICloudOutboxMessageRepository cloudOutboxMessages,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        IssueInvoiceValidator validator)
    {
        _invoices = invoices;
        _clients = clients;
        _chargeCodes = chargeCodes;
        _clientAccountingProfiles = clientAccountingProfiles;
        _ledgerAccounts = ledgerAccounts;
        _journalEntries = journalEntries;
        _periodGuard = periodGuard;
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

            var client = await _clients.GetByIdAsync(invoice.ClientId, cancellationToken);

            if (client is null)
            {
                return Result<IssueInvoiceResult>.Failure(ApplicationError.NotFound(
                    nameof(invoice.ClientId),
                    "Invoice client was not found."));
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

            var postingLines = await BuildPostingLinesAsync(invoice, cancellationToken);

            if (postingLines.IsFailure)
            {
                return Result<IssueInvoiceResult>.Failure(postingLines.Errors);
            }

            var periodError = await _periodGuard.ValidateOpenPeriodAsync(
                command.PostingDate,
                nameof(command.PostingDate),
                cancellationToken: cancellationToken);

            if (periodError is not null)
            {
                return Result<IssueInvoiceResult>.Failure(periodError);
            }

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var journalEntry = CreateJournalEntry(invoice, receivableAccountId, postingLines.Value, command.PostingDate);

                    invoice.Issue();
                    journalEntry.Post(_clock.UtcNow);

                    await _journalEntries.AddAsync(journalEntry, token);
                    await _cloudOutboxMessages.AddAsync(
                        CreateInvoiceIssuedOutboxMessage(invoice, client, journalEntry, receivableAccountId),
                        token);

                    return await ToResultAsync(invoice, journalEntry, token);
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

    private async Task<Result<InvoicePostingLines>> BuildPostingLinesAsync(
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        var revenueLines = new List<AccountPostingLine>();
        var taxLines = new List<AccountPostingLine>();

        foreach (var invoiceLine in invoice.Lines)
        {
            if (!invoiceLine.ChargeCodeId.HasValue)
            {
                return Result<InvoicePostingLines>.Failure(ApplicationError.Validation(
                    nameof(invoiceLine.ChargeCodeId),
                    "Every invoice line must reference a charge code before posting."));
            }

            var chargeCode = await _chargeCodes.GetByIdAsync(invoiceLine.ChargeCodeId.Value, cancellationToken);

            if (chargeCode is null)
            {
                return Result<InvoicePostingLines>.Failure(ApplicationError.NotFound(
                    nameof(invoiceLine.ChargeCodeId),
                    "Charge code for an invoice line was not found."));
            }

            if (invoiceLine.LineType == InvoiceLineType.Tax)
            {
                if (!chargeCode.TaxAccountId.HasValue)
                {
                    return Result<InvoicePostingLines>.Failure(ApplicationError.Validation(
                        nameof(chargeCode.TaxAccountId),
                        $"Charge code {chargeCode.Code.Value} needs a tax account before posting tax."));
                }

                taxLines.Add(new AccountPostingLine(
                    chargeCode.TaxAccountId.Value,
                    invoiceLine.Amount,
                    invoiceLine.Description));

                continue;
            }

            revenueLines.Add(new AccountPostingLine(
                chargeCode.RevenueAccountId,
                invoiceLine.Amount,
                invoiceLine.Description));
        }

        var accountValidationErrors = new List<ApplicationError>();
        accountValidationErrors.AddRange(await ValidatePostingAccountsAsync(
            revenueLines.Select(line => line.LedgerAccountId),
            LedgerAccountType.Revenue,
            "Revenue ledger account",
            cancellationToken));
        accountValidationErrors.AddRange(await ValidatePostingAccountsAsync(
            taxLines.Select(line => line.LedgerAccountId),
            LedgerAccountType.Liability,
            "Tax ledger account",
            cancellationToken));

        if (accountValidationErrors.Count > 0)
        {
            return Result<InvoicePostingLines>.Failure(accountValidationErrors);
        }

        return Result<InvoicePostingLines>.Success(new InvoicePostingLines(revenueLines, taxLines));
    }

    private JournalEntry CreateJournalEntry(
        Invoice invoice,
        LedgerAccountId receivableAccountId,
        InvoicePostingLines postingLines,
        DateOnly postingDate)
    {
        var journalEntry = JournalEntry.Create(
            JournalEntryId.Create(_idGenerator.NewGuid()),
            postingDate,
            invoice.CurrencyCode,
            JournalSourceType.BillingInvoice,
            invoice.Number.Value,
            $"Invoice {invoice.Number.Value}",
            _clock.UtcNow,
            invoice.ClientId,
            invoice.Id.Value);

        journalEntry.AddLine(JournalLine.DebitLine(
            receivableAccountId,
            invoice.TotalAmount,
            $"Accounts receivable for invoice {invoice.Number.Value}"));

        foreach (var group in postingLines.RevenueLines.GroupBy(line => line.LedgerAccountId))
        {
            var amount = group
                .Select(line => line.Amount)
                .Aggregate((total, amount) => total.Add(amount));

            var description = group.Count() == 1
                ? group.First().Description
                : $"Revenue for invoice {invoice.Number.Value}";

            journalEntry.AddLine(JournalLine.CreditLine(group.Key, amount, description));
        }

        foreach (var group in postingLines.TaxLines.GroupBy(line => line.LedgerAccountId))
        {
            var amount = group
                .Select(line => line.Amount)
                .Aggregate((total, amount) => total.Add(amount));

            var description = group.Count() == 1
                ? group.First().Description
                : $"Tax payable for invoice {invoice.Number.Value}";

            journalEntry.AddLine(JournalLine.CreditLine(group.Key, amount, description));
        }

        return journalEntry;
    }

    private async Task<IReadOnlyCollection<ApplicationError>> ValidatePostingAccountsAsync(
        IEnumerable<LedgerAccountId> ledgerAccountIds,
        LedgerAccountType expectedType,
        string accountRole,
        CancellationToken cancellationToken)
    {
        var errors = new List<ApplicationError>();

        foreach (var ledgerAccountId in ledgerAccountIds.Distinct())
        {
            var ledgerAccount = await _ledgerAccounts.GetByIdAsync(ledgerAccountId, cancellationToken);

            if (ledgerAccount is null)
            {
                errors.Add(ApplicationError.NotFound(
                    nameof(ledgerAccountId),
                    $"{accountRole} was not found."));
                continue;
            }

            if (ledgerAccount.Type != expectedType)
            {
                errors.Add(ApplicationError.Validation(
                    nameof(ledgerAccountId),
                    $"{accountRole} must be a {expectedType.ToString().ToLowerInvariant()} account."));
            }

            if (!ledgerAccount.IsPostingAccount)
            {
                errors.Add(ApplicationError.Validation(
                    nameof(ledgerAccountId),
                    $"{accountRole} must be a posting account."));
            }

            if (ledgerAccount.Status != LedgerAccountStatus.Active)
            {
                errors.Add(ApplicationError.Validation(
                    nameof(ledgerAccountId),
                    $"{accountRole} must be active."));
            }
        }

        return errors;
    }

    private CloudOutboxMessage CreateInvoiceIssuedOutboxMessage(
        Invoice invoice,
        Client client,
        JournalEntry journalEntry,
        LedgerAccountId receivableAccountId)
    {
        var billingContact = client.Contacts
            .OrderBy(contact => contact.Role is ClientContactRole.Billing or ClientContactRole.Accounts ? 0 : 1)
            .ThenByDescending(contact => contact.IsPrimary)
            .FirstOrDefault();
        var payload = new InvoiceIssuedCloudPayload(
            "2",
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
                line.LineType.ToString(),
                line.Description,
                1m,
                line.Amount.Amount,
                line.Amount.Amount,
                line.Amount.Amount,
                line.Amount.CurrencyCode)).ToArray(),
            new InvoiceIssuedCloudPayloadClient(
                client.DisplayName,
                billingContact?.FullName,
                billingContact?.Email,
                billingContact?.Phone));

        return CloudOutboxMessage.Create(
            CloudOutboxMessageId.Create(_idGenerator.NewGuid()),
            invoice.ClientId,
            "InvoiceIssued",
            "Invoice",
            invoice.Id.Value.ToString(),
            JsonSerializer.Serialize(payload, JsonOptions),
            _clock.UtcNow);
    }

    private async Task<IssueInvoiceResult> ToResultAsync(
        Invoice invoice,
        JournalEntry journalEntry,
        CancellationToken cancellationToken)
    {
        var ledgerAccountsById = JournalLineLedgerAccountMetadataFactory.ToLookup(
            await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken));

        return BillingDocumentResultFactory.ToIssueInvoiceResult(invoice, journalEntry, ledgerAccountsById);
    }

    private sealed record InvoicePostingLines(
        IReadOnlyCollection<AccountPostingLine> RevenueLines,
        IReadOnlyCollection<AccountPostingLine> TaxLines);

    private sealed record AccountPostingLine(
        LedgerAccountId LedgerAccountId,
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
        IReadOnlyCollection<InvoiceIssuedCloudPayloadLine> Lines,
        InvoiceIssuedCloudPayloadClient Client);

    private sealed record InvoiceIssuedCloudPayloadLine(
        Guid? ChargeCodeId,
        string LineType,
        string Description,
        decimal Quantity,
        decimal UnitPrice,
        decimal LineTotal,
        decimal Amount,
        string CurrencyCode);

    private sealed record InvoiceIssuedCloudPayloadClient(
        string Name,
        string? ContactName,
        string? Email,
        string? Phone);
}
