using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountingPeriods;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ReopenAccountingPeriod;

public sealed class ReopenAccountingPeriodHandler
{
    private readonly IAccountingPeriodRepository _periods;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public ReopenAccountingPeriodHandler(
        IAccountingPeriodRepository periods,
        IJournalEntryRepository journalEntries,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _periods = periods;
        _journalEntries = journalEntries;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<Result<AccountingPeriodResult>> HandleAsync(
        ReopenAccountingPeriodCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AccountingPeriodId == Guid.Empty)
        {
            return Result<AccountingPeriodResult>.Failure(ApplicationError.Validation(
                nameof(command.AccountingPeriodId),
                "Accounting period id cannot be empty."));
        }

        var period = await _periods.GetByIdAsync(
            AccountingPeriodId.Create(command.AccountingPeriodId),
            cancellationToken);

        if (period is null)
        {
            return Result<AccountingPeriodResult>.Failure(ApplicationError.NotFound(
                nameof(command.AccountingPeriodId),
                "Accounting period was not found."));
        }

        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            period.CompanyCode,
            nameof(AccountingPeriod.CompanyCode));

        if (companyError is not null)
        {
            return Result<AccountingPeriodResult>.Failure(companyError);
        }

        if (period.Status == AccountingPeriodStatus.Open)
        {
            return Result<AccountingPeriodResult>.Success(ListAccountingPeriodsHandler.ToResult(period));
        }

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var reopenedAtUtc = _clock.UtcNow;
                    var closeJournals = await _journalEntries.ListAsync(
                        period.StartsOn,
                        period.EndsOn,
                        JournalSourceType.PeriodClose,
                        token);

                    foreach (var closeJournal in closeJournals.Where(entry => entry.Status == JournalEntryStatus.Posted))
                    {
                        var reversalJournal = CreateCloseJournalReversal(
                            period,
                            closeJournal,
                            reopenedAtUtc);

                        closeJournal.Void(reopenedAtUtc);
                        reversalJournal.Post(reopenedAtUtc);

                        await _journalEntries.AddAsync(reversalJournal, token);
                    }

                    period.Reopen(reopenedAtUtc);
                },
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Result<AccountingPeriodResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<AccountingPeriodResult>.Failure(ApplicationError.Validation(
                nameof(command.AccountingPeriodId),
                exception.Message));
        }

        return Result<AccountingPeriodResult>.Success(ListAccountingPeriodsHandler.ToResult(period));
    }

    private JournalEntry CreateCloseJournalReversal(
        AccountingPeriod period,
        JournalEntry closeJournal,
        DateTimeOffset createdAtUtc)
    {
        var sourceReference = closeJournal.Id.Value.ToString();
        var reversalJournal = JournalEntry.Create(
            JournalEntryId.Create(_idGenerator.NewGuid()),
            period.EndsOn,
            closeJournal.CurrencyCode,
            JournalSourceType.PeriodCloseReversal,
            sourceReference,
            $"Reopen {period.Name}: reverse close journal {closeJournal.SourceReference ?? sourceReference}",
            createdAtUtc);

        foreach (var line in closeJournal.Lines)
        {
            var description = $"Reopen {line.Description ?? closeJournal.Memo ?? closeJournal.SourceReference ?? sourceReference}";

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
}
