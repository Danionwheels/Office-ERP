using System.Text.Json;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Common;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.VoidInvoice;

public sealed class VoidInvoiceHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IInvoiceRepository _invoices;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly AccountingPeriodPostingGuard _periodGuard;
    private readonly ICloudOutboxMessageRepository _cloudOutboxMessages;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly VoidInvoiceValidator _validator;

    public VoidInvoiceHandler(
        IInvoiceRepository invoices,
        IJournalEntryRepository journalEntries,
        ILedgerAccountRepository ledgerAccounts,
        AccountingPeriodPostingGuard periodGuard,
        ICloudOutboxMessageRepository cloudOutboxMessages,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        VoidInvoiceValidator validator)
    {
        _invoices = invoices;
        _journalEntries = journalEntries;
        _ledgerAccounts = ledgerAccounts;
        _periodGuard = periodGuard;
        _cloudOutboxMessages = cloudOutboxMessages;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<VoidInvoiceResult>> HandleAsync(
        VoidInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<VoidInvoiceResult>.Failure(validationErrors);
        }

        try
        {
            var invoice = await _invoices.GetByIdAsync(InvoiceId.Create(command.InvoiceId), cancellationToken);

            if (invoice is null)
            {
                return Result<VoidInvoiceResult>.Failure(ApplicationError.NotFound(
                    nameof(command.InvoiceId),
                    "Invoice was not found."));
            }

            if (invoice.Status != InvoiceStatus.Issued || invoice.AmountPaid.Amount > 0)
            {
                return Result<VoidInvoiceResult>.Failure(ApplicationError.Validation(
                    nameof(command.InvoiceId),
                    "Only unpaid issued invoices can be voided."));
            }

            var originalJournal = await FindOriginalInvoiceJournalAsync(invoice, cancellationToken);

            if (originalJournal is null)
            {
                return Result<VoidInvoiceResult>.Failure(ApplicationError.NotFound(
                    nameof(command.InvoiceId),
                    "Original posted invoice journal entry was not found."));
            }

            if (await HasExistingVoidJournalAsync(invoice, cancellationToken))
            {
                return Result<VoidInvoiceResult>.Failure(ApplicationError.Conflict(
                    nameof(command.InvoiceId),
                    "Invoice already has a void journal entry."));
            }

            var periodError = await _periodGuard.ValidateOpenPeriodAsync(
                command.VoidDate,
                nameof(command.VoidDate),
                cancellationToken: cancellationToken);

            if (periodError is not null)
            {
                return Result<VoidInvoiceResult>.Failure(periodError);
            }

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var reversalJournal = CreateReversalJournal(invoice, originalJournal, command);

                    invoice.Void();
                    reversalJournal.Post(_clock.UtcNow);

                    await _journalEntries.AddAsync(reversalJournal, token);
                    await _cloudOutboxMessages.AddAsync(
                        CreateInvoiceVoidedOutboxMessage(invoice, originalJournal, reversalJournal, command),
                        token);

                    return await ToResultAsync(invoice, originalJournal, reversalJournal, command.VoidDate, token);
                },
                cancellationToken);

            return Result<VoidInvoiceResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<VoidInvoiceResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<VoidInvoiceResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private async Task<JournalEntry?> FindOriginalInvoiceJournalAsync(
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        var journalEntries = await _journalEntries.ListForSourceDocumentAsync(
            JournalSourceType.BillingInvoice,
            invoice.Id.Value,
            cancellationToken);

        return journalEntries
            .Where(entry => entry.Status == JournalEntryStatus.Posted)
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.Id.Value)
            .FirstOrDefault();
    }

    private async Task<bool> HasExistingVoidJournalAsync(
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        var journalEntries = await _journalEntries.ListForSourceDocumentAsync(
            JournalSourceType.BillingInvoiceVoid,
            invoice.Id.Value,
            cancellationToken);

        return journalEntries.Count > 0;
    }

    private JournalEntry CreateReversalJournal(
        Invoice invoice,
        JournalEntry originalJournal,
        VoidInvoiceCommand command)
    {
        var journalEntry = JournalEntry.Create(
            JournalEntryId.Create(_idGenerator.NewGuid()),
            command.VoidDate,
            originalJournal.CurrencyCode,
            JournalSourceType.BillingInvoiceVoid,
            invoice.Number.Value,
            $"Void invoice {invoice.Number.Value}: {command.Reason.Trim()}",
            _clock.UtcNow,
            invoice.ClientId,
            invoice.Id.Value);

        foreach (var line in originalJournal.Lines)
        {
            if (line.Debit.Amount > 0)
            {
                journalEntry.AddLine(JournalLine.CreditLine(
                    line.LedgerAccountId,
                    line.Debit,
                    $"Void {line.Description ?? invoice.Number.Value}"));
            }

            if (line.Credit.Amount > 0)
            {
                journalEntry.AddLine(JournalLine.DebitLine(
                    line.LedgerAccountId,
                    line.Credit,
                    $"Void {line.Description ?? invoice.Number.Value}"));
            }
        }

        return journalEntry;
    }

    private CloudOutboxMessage CreateInvoiceVoidedOutboxMessage(
        Invoice invoice,
        JournalEntry originalJournal,
        JournalEntry reversalJournal,
        VoidInvoiceCommand command)
    {
        var payload = new InvoiceVoidedCloudPayload(
            "1",
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.ClientId.Value,
            invoice.ContractId.Value,
            invoice.Status.ToString(),
            command.VoidDate,
            command.Reason.Trim(),
            invoice.TotalAmount.Amount,
            invoice.BalanceDue.Amount,
            invoice.CurrencyCode,
            originalJournal.Id.Value,
            reversalJournal.Id.Value,
            reversalJournal.Status.ToString());

        return CloudOutboxMessage.Create(
            CloudOutboxMessageId.Create(_idGenerator.NewGuid()),
            invoice.ClientId,
            "InvoiceVoided",
            "Invoice",
            invoice.Id.Value.ToString(),
            JsonSerializer.Serialize(payload, JsonOptions),
            _clock.UtcNow);
    }

    private async Task<VoidInvoiceResult> ToResultAsync(
        Invoice invoice,
        JournalEntry originalJournal,
        JournalEntry reversalJournal,
        DateOnly voidDate,
        CancellationToken cancellationToken)
    {
        var ledgerAccountsById = JournalLineLedgerAccountMetadataFactory.ToLookup(
            await _ledgerAccounts.ListAsync(cancellationToken: cancellationToken));

        return BillingDocumentResultFactory.ToVoidInvoiceResult(
            invoice,
            originalJournal,
            reversalJournal,
            ledgerAccountsById);
    }

    private sealed record InvoiceVoidedCloudPayload(
        string EventVersion,
        Guid InvoiceId,
        string InvoiceNumber,
        Guid ClientId,
        Guid ContractId,
        string InvoiceStatus,
        DateOnly VoidDate,
        string Reason,
        decimal TotalAmount,
        decimal BalanceDue,
        string CurrencyCode,
        Guid OriginalJournalEntryId,
        Guid ReversalJournalEntryId,
        string ReversalJournalEntryStatus);
}
