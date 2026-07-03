using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PostManualJournalEntry;

public sealed class PostManualJournalEntryHandler
{
    private readonly IJournalEntryRepository _journalEntries;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly AccountingPeriodPostingGuard _periodGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly PostManualJournalEntryValidator _validator;

    public PostManualJournalEntryHandler(
        IJournalEntryRepository journalEntries,
        ILedgerAccountRepository ledgerAccounts,
        AccountingPeriodPostingGuard periodGuard,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        PostManualJournalEntryValidator validator)
    {
        _journalEntries = journalEntries;
        _ledgerAccounts = ledgerAccounts;
        _periodGuard = periodGuard;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<PostManualJournalEntryResult>> HandleAsync(
        PostManualJournalEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<PostManualJournalEntryResult>.Failure(validationErrors);
        }

        try
        {
            var periodError = await _periodGuard.ValidateOpenPeriodAsync(
                command.EntryDate,
                nameof(command.EntryDate),
                cancellationToken: cancellationToken);

            if (periodError is not null)
            {
                return Result<PostManualJournalEntryResult>.Failure(periodError);
            }

            var accountErrors = await ValidateAccountsAsync(command, cancellationToken);

            if (accountErrors.Count > 0)
            {
                return Result<PostManualJournalEntryResult>.Failure(accountErrors);
            }

            var journalEntry = JournalEntry.Create(
                JournalEntryId.Create(_idGenerator.NewGuid()),
                command.EntryDate,
                command.CurrencyCode,
                JournalSourceType.Manual,
                command.SourceReference,
                command.Memo,
                _clock.UtcNow);

            foreach (var line in command.Lines)
            {
                var ledgerAccountId = LedgerAccountId.Create(line.LedgerAccountId);
                var description = line.Description;

                if (line.Debit > 0)
                {
                    journalEntry.AddLine(JournalLine.DebitLine(
                        ledgerAccountId,
                        Money.Of(line.Debit, journalEntry.CurrencyCode),
                        description));
                }
                else
                {
                    journalEntry.AddLine(JournalLine.CreditLine(
                        ledgerAccountId,
                        Money.Of(line.Credit, journalEntry.CurrencyCode),
                        description));
                }
            }

            journalEntry.Post(_clock.UtcNow);

            await _journalEntries.AddAsync(journalEntry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<PostManualJournalEntryResult>.Success(ToResult(journalEntry));
        }
        catch (ArgumentException exception)
        {
            return Result<PostManualJournalEntryResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<PostManualJournalEntryResult>.Failure(ApplicationError.Validation(
                nameof(command.Lines),
                exception.Message));
        }
    }

    private async Task<IReadOnlyCollection<ApplicationError>> ValidateAccountsAsync(
        PostManualJournalEntryCommand command,
        CancellationToken cancellationToken)
    {
        var errors = new List<ApplicationError>();
        var uniqueAccountIds = command.Lines
            .Select(line => line.LedgerAccountId)
            .Where(accountId => accountId != Guid.Empty)
            .Distinct()
            .ToArray();

        foreach (var accountId in uniqueAccountIds)
        {
            var ledgerAccount = await _ledgerAccounts.GetByIdAsync(
                LedgerAccountId.Create(accountId),
                cancellationToken);

            if (ledgerAccount is null)
            {
                errors.Add(ApplicationError.NotFound(
                    nameof(PostManualJournalEntryLineCommand.LedgerAccountId),
                    "Journal line ledger account was not found."));
                continue;
            }

            if (!ledgerAccount.IsPostingAccount)
            {
                errors.Add(ApplicationError.Validation(
                    nameof(PostManualJournalEntryLineCommand.LedgerAccountId),
                    $"Ledger account {ledgerAccount.Code.Value} must be a posting account."));
            }

            if (ledgerAccount.Status != LedgerAccountStatus.Active)
            {
                errors.Add(ApplicationError.Validation(
                    nameof(PostManualJournalEntryLineCommand.LedgerAccountId),
                    $"Ledger account {ledgerAccount.Code.Value} must be active."));
            }
        }

        return errors;
    }

    private static PostManualJournalEntryResult ToResult(JournalEntry entry)
    {
        return new PostManualJournalEntryResult(
            entry.Id.Value,
            entry.EntryDate,
            entry.CurrencyCode,
            entry.SourceType.ToString(),
            entry.SourceReference,
            entry.Memo,
            entry.Status.ToString(),
            entry.TotalDebit.Amount,
            entry.TotalCredit.Amount,
            entry.Lines.Select(line => new PostManualJournalEntryLineResult(
                line.LedgerAccountId.Value,
                line.Debit.Amount,
                line.Credit.Amount,
                line.Description)).ToArray());
    }
}
