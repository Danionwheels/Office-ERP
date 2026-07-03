using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.VoidManualJournalEntry;

public sealed class VoidManualJournalEntryHandler
{
    private readonly IJournalEntryRepository _journalEntries;
    private readonly AccountingPeriodPostingGuard _periodGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly VoidManualJournalEntryValidator _validator;

    public VoidManualJournalEntryHandler(
        IJournalEntryRepository journalEntries,
        AccountingPeriodPostingGuard periodGuard,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        VoidManualJournalEntryValidator validator)
    {
        _journalEntries = journalEntries;
        _periodGuard = periodGuard;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<VoidManualJournalEntryResult>> HandleAsync(
        VoidManualJournalEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<VoidManualJournalEntryResult>.Failure(validationErrors);
        }

        try
        {
            var periodError = await _periodGuard.ValidateOpenPeriodAsync(
                command.VoidDate,
                nameof(command.VoidDate),
                cancellationToken: cancellationToken);

            if (periodError is not null)
            {
                return Result<VoidManualJournalEntryResult>.Failure(periodError);
            }

            var originalJournal = await _journalEntries.GetByIdAsync(
                JournalEntryId.Create(command.JournalEntryId),
                cancellationToken);

            if (originalJournal is null)
            {
                return Result<VoidManualJournalEntryResult>.Failure(ApplicationError.NotFound(
                    nameof(command.JournalEntryId),
                    "Journal entry was not found."));
            }

            if (originalJournal.SourceType != JournalSourceType.Manual)
            {
                return Result<VoidManualJournalEntryResult>.Failure(ApplicationError.Validation(
                    nameof(command.JournalEntryId),
                    "Only manual journal entries can be voided from the GL workbench."));
            }

            if (originalJournal.Status != JournalEntryStatus.Posted)
            {
                return Result<VoidManualJournalEntryResult>.Failure(ApplicationError.Validation(
                    nameof(command.JournalEntryId),
                    "Only posted manual journal entries can be voided."));
            }

            if (await HasExistingReversalAsync(originalJournal, cancellationToken))
            {
                return Result<VoidManualJournalEntryResult>.Failure(ApplicationError.Conflict(
                    nameof(command.JournalEntryId),
                    "Manual journal entry already has a reversal."));
            }

            var result = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var reversalJournal = CreateReversalJournal(originalJournal, command);

                    originalJournal.Void(_clock.UtcNow);
                    reversalJournal.Post(_clock.UtcNow);

                    await _journalEntries.AddAsync(reversalJournal, token);

                    return ToResult(originalJournal, reversalJournal, command.VoidDate);
                },
                cancellationToken);

            return Result<VoidManualJournalEntryResult>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return Result<VoidManualJournalEntryResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<VoidManualJournalEntryResult>.Failure(ApplicationError.Validation(
                nameof(command),
                exception.Message));
        }
    }

    private async Task<bool> HasExistingReversalAsync(
        JournalEntry originalJournal,
        CancellationToken cancellationToken)
    {
        var reversals = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.ManualReversal,
            cancellationToken: cancellationToken);

        return reversals.Any(entry =>
            string.Equals(entry.SourceReference, originalJournal.Id.Value.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    private JournalEntry CreateReversalJournal(
        JournalEntry originalJournal,
        VoidManualJournalEntryCommand command)
    {
        var reversalJournal = JournalEntry.Create(
            JournalEntryId.Create(_idGenerator.NewGuid()),
            command.VoidDate,
            originalJournal.CurrencyCode,
            JournalSourceType.ManualReversal,
            originalJournal.Id.Value.ToString(),
            $"Void manual journal {originalJournal.SourceReference ?? originalJournal.Id.Value.ToString()}: {command.Reason.Trim()}",
            _clock.UtcNow);

        foreach (var line in originalJournal.Lines)
        {
            var description = $"Void {line.Description ?? originalJournal.Memo ?? originalJournal.SourceReference ?? originalJournal.Id.Value.ToString()}";

            if (line.Debit.Amount > 0)
            {
                reversalJournal.AddLine(JournalLine.CreditLine(
                    line.LedgerAccountId,
                    line.Debit,
                    description));
            }

            if (line.Credit.Amount > 0)
            {
                reversalJournal.AddLine(JournalLine.DebitLine(
                    line.LedgerAccountId,
                    line.Credit,
                    description));
            }
        }

        return reversalJournal;
    }

    private static VoidManualJournalEntryResult ToResult(
        JournalEntry originalJournal,
        JournalEntry reversalJournal,
        DateOnly voidDate)
    {
        return new VoidManualJournalEntryResult(
            originalJournal.Id.Value,
            reversalJournal.Id.Value,
            originalJournal.Status.ToString(),
            reversalJournal.Status.ToString(),
            voidDate,
            reversalJournal.TotalDebit.Amount,
            reversalJournal.TotalCredit.Amount,
            reversalJournal.CurrencyCode,
            reversalJournal.Lines.Select(line => new VoidManualJournalEntryLineResult(
                line.LedgerAccountId.Value,
                line.Debit.Amount,
                line.Credit.Amount,
                line.Description)).ToArray());
    }
}
