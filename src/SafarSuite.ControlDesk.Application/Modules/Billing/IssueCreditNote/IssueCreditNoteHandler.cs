using System.Text.Json;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.IssueCreditNote;

public sealed class IssueCreditNoteHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IInvoiceRepository _invoices;
    private readonly ICreditNoteRepository _creditNotes;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly AccountingPeriodPostingGuard _periodGuard;
    private readonly ICloudOutboxMessageRepository _cloudOutboxMessages;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly IssueCreditNoteValidator _validator;

    public IssueCreditNoteHandler(
        IInvoiceRepository invoices,
        ICreditNoteRepository creditNotes,
        IJournalEntryRepository journalEntries,
        AccountingPeriodPostingGuard periodGuard,
        ICloudOutboxMessageRepository cloudOutboxMessages,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        IssueCreditNoteValidator validator)
    {
        _invoices = invoices;
        _creditNotes = creditNotes;
        _journalEntries = journalEntries;
        _periodGuard = periodGuard;
        _cloudOutboxMessages = cloudOutboxMessages;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<IssueCreditNoteResult>> HandleAsync(
        IssueCreditNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<IssueCreditNoteResult>.Failure(validationErrors);
        }

        try
        {
            var invoice = await _invoices.GetByIdAsync(InvoiceId.Create(command.InvoiceId), cancellationToken);

            if (invoice is null)
            {
                return Result<IssueCreditNoteResult>.Failure(ApplicationError.NotFound(
                    nameof(command.InvoiceId),
                    "Invoice was not found."));
            }

            if (invoice.Status is not (InvoiceStatus.Paid or InvoiceStatus.PartiallyPaid))
            {
                return Result<IssueCreditNoteResult>.Failure(ApplicationError.Validation(
                    nameof(command.InvoiceId),
                    "Credit notes can only be issued for paid or partially paid invoices. Use invoice void for unpaid issued invoices."));
            }

            if (command.CreditDate < invoice.IssueDate)
            {
                return Result<IssueCreditNoteResult>.Failure(ApplicationError.Validation(
                    nameof(command.CreditDate),
                    "Credit note date cannot be before invoice issue date."));
            }

            var creditNoteNumber = CreditNoteNumber.Create(command.CreditNoteNumber);

            if (await _creditNotes.ExistsByNumberAsync(creditNoteNumber, cancellationToken))
            {
                return Result<IssueCreditNoteResult>.Failure(ApplicationError.Conflict(
                    nameof(command.CreditNoteNumber),
                    $"Credit note {creditNoteNumber.Value} already exists."));
            }

            if (await _creditNotes.ExistsForInvoiceAsync(invoice.Id, cancellationToken))
            {
                return Result<IssueCreditNoteResult>.Failure(ApplicationError.Conflict(
                    nameof(command.InvoiceId),
                    "Invoice already has a full credit note."));
            }

            var originalJournal = await FindOriginalInvoiceJournalAsync(invoice, cancellationToken);

            if (originalJournal is null)
            {
                return Result<IssueCreditNoteResult>.Failure(ApplicationError.NotFound(
                    nameof(command.InvoiceId),
                    "Original posted invoice journal entry was not found."));
            }

            var periodError = await _periodGuard.ValidateOpenPeriodAsync(
                command.CreditDate,
                nameof(command.CreditDate),
                cancellationToken: cancellationToken);

            if (periodError is not null)
            {
                return Result<IssueCreditNoteResult>.Failure(periodError);
            }

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var creditNote = CreditNote.Create(
                        CreditNoteId.Create(_idGenerator.NewGuid()),
                        invoice,
                        creditNoteNumber,
                        command.CreditDate,
                        command.Reason,
                        _clock.UtcNow);
                    var journalEntry = CreateCreditNoteJournalEntry(creditNote, invoice, originalJournal);

                    journalEntry.Post(_clock.UtcNow);

                    await _creditNotes.AddAsync(creditNote, token);
                    await _journalEntries.AddAsync(journalEntry, token);
                    await _cloudOutboxMessages.AddAsync(
                        CreateCreditNoteIssuedOutboxMessage(creditNote, invoice, journalEntry, originalJournal),
                        token);

                    return ToResult(creditNote, invoice, journalEntry);
                },
                cancellationToken);

            return Result<IssueCreditNoteResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<IssueCreditNoteResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<IssueCreditNoteResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private async Task<JournalEntry?> FindOriginalInvoiceJournalAsync(
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        var journalEntries = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.BillingInvoice,
            cancellationToken: cancellationToken);

        return journalEntries
            .Where(entry => entry.SourceReference == invoice.Number.Value)
            .Where(entry => entry.Status == JournalEntryStatus.Posted)
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.Id.Value)
            .FirstOrDefault();
    }

    private JournalEntry CreateCreditNoteJournalEntry(
        CreditNote creditNote,
        Invoice invoice,
        JournalEntry originalJournal)
    {
        var journalEntry = JournalEntry.Create(
            JournalEntryId.Create(_idGenerator.NewGuid()),
            creditNote.CreditDate,
            originalJournal.CurrencyCode,
            JournalSourceType.BillingCreditNote,
            creditNote.Number.Value,
            $"Credit note {creditNote.Number.Value} for invoice {invoice.Number.Value}: {creditNote.Reason}",
            _clock.UtcNow);

        foreach (var line in originalJournal.Lines)
        {
            var description = $"Credit note {creditNote.Number.Value}: {line.Description ?? invoice.Number.Value}";

            if (line.IsDebit)
            {
                journalEntry.AddLine(JournalLine.CreditLine(line.LedgerAccountId, line.Debit, description));
            }
            else if (line.IsCredit)
            {
                journalEntry.AddLine(JournalLine.DebitLine(line.LedgerAccountId, line.Credit, description));
            }
        }

        return journalEntry;
    }

    private CloudOutboxMessage CreateCreditNoteIssuedOutboxMessage(
        CreditNote creditNote,
        Invoice invoice,
        JournalEntry journalEntry,
        JournalEntry originalJournal)
    {
        var payload = new CreditNoteIssuedCloudPayload(
            "1",
            creditNote.Id.Value,
            creditNote.Number.Value,
            invoice.Id.Value,
            invoice.Number.Value,
            invoice.ClientId.Value,
            invoice.ContractId.Value,
            creditNote.Status.ToString(),
            creditNote.CreditDate,
            creditNote.Reason,
            creditNote.TotalAmount.Amount,
            creditNote.CurrencyCode,
            journalEntry.Id.Value,
            journalEntry.EntryDate,
            journalEntry.Status.ToString(),
            originalJournal.Id.Value);

        return CloudOutboxMessage.Create(
            CloudOutboxMessageId.Create(_idGenerator.NewGuid()),
            "CreditNoteIssued",
            "CreditNote",
            creditNote.Id.Value.ToString(),
            JsonSerializer.Serialize(payload, JsonOptions),
            _clock.UtcNow);
    }

    private static IssueCreditNoteResult ToResult(
        CreditNote creditNote,
        Invoice invoice,
        JournalEntry journalEntry)
    {
        return new IssueCreditNoteResult(
            creditNote.Id.Value,
            invoice.Id.Value,
            creditNote.Number.Value,
            invoice.Number.Value,
            creditNote.Status.ToString(),
            creditNote.CreditDate,
            creditNote.TotalAmount.Amount,
            creditNote.CurrencyCode,
            journalEntry.Id.Value,
            journalEntry.Status.ToString(),
            journalEntry.TotalDebit.Amount,
            journalEntry.TotalCredit.Amount,
            journalEntry.Lines.Select(line => new IssueCreditNoteJournalLineResult(
                line.LedgerAccountId.Value,
                line.Debit.Amount,
                line.Credit.Amount,
                line.Description)).ToArray());
    }

    private sealed record CreditNoteIssuedCloudPayload(
        string EventVersion,
        Guid CreditNoteId,
        string CreditNoteNumber,
        Guid InvoiceId,
        string InvoiceNumber,
        Guid ClientId,
        Guid ContractId,
        string CreditNoteStatus,
        DateOnly CreditDate,
        string Reason,
        decimal Amount,
        string CurrencyCode,
        Guid JournalEntryId,
        DateOnly PostingDate,
        string JournalEntryStatus,
        Guid OriginalJournalEntryId);
}
