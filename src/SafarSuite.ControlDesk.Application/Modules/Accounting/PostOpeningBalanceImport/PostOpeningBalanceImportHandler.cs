using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureOpeningBalanceProfile;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListJournalEntries;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Application.Modules.Accounting.PreviewOpeningBalanceImport;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.PostOpeningBalanceImport;

public sealed class PostOpeningBalanceImportHandler
{
    private readonly PreviewOpeningBalanceImportHandler _previewOpeningBalanceImport;
    private readonly ConfigureOpeningBalanceProfileHandler _configureOpeningBalanceProfile;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public PostOpeningBalanceImportHandler(
        PreviewOpeningBalanceImportHandler previewOpeningBalanceImport,
        ConfigureOpeningBalanceProfileHandler configureOpeningBalanceProfile,
        IJournalEntryRepository journalEntries,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _previewOpeningBalanceImport = previewOpeningBalanceImport;
        _configureOpeningBalanceProfile = configureOpeningBalanceProfile;
        _journalEntries = journalEntries;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<Result<JournalEntrySummaryResult>> HandleAsync(
        PostOpeningBalanceImportCommand command,
        CancellationToken cancellationToken = default)
    {
        var previewResult = await _previewOpeningBalanceImport.HandleAsync(
            new PreviewOpeningBalanceImportCommand(
                command.EntryDate,
                command.CurrencyCode,
                command.SourceReference,
                command.Memo,
                command.ProfileFromDate,
                command.ProfileToDate,
                command.ProfileStatus,
                command.TransactionsAllowed,
                command.ProfitAndLossCarryForwardAccountId,
                command.Lines?.Select(line => new PreviewOpeningBalanceImportLineCommand(
                    line.AccountCode,
                    line.Debit,
                    line.Credit,
                    line.Description)).ToArray() ?? []),
            cancellationToken);

        if (previewResult.IsFailure)
        {
            return Result<JournalEntrySummaryResult>.Failure(previewResult.Errors);
        }

        var preview = previewResult.Value;

        if (!preview.CanPost)
        {
            return Result<JournalEntrySummaryResult>.Failure(
                preview.Blockers.Select(blocker => ApplicationError.Validation(
                    nameof(command.Lines),
                    blocker)));
        }

        var duplicateError = await ValidateUniqueSourceReferenceAsync(
            preview.SourceReference,
            cancellationToken);

        if (duplicateError is not null)
        {
            return Result<JournalEntrySummaryResult>.Failure(duplicateError);
        }

        var profileResult = await _configureOpeningBalanceProfile.HandleAsync(
            new ConfigureOpeningBalanceProfileCommand(
                null,
                command.ProfileFromDate,
                command.ProfileToDate,
                command.ProfileStatus,
                command.TransactionsAllowed,
                command.ProfitAndLossCarryForwardAccountId),
            cancellationToken);

        if (profileResult.IsFailure)
        {
            return Result<JournalEntrySummaryResult>.Failure(profileResult.Errors);
        }

        try
        {
            var journalEntry = JournalEntry.Create(
                JournalEntryId.Create(_idGenerator.NewGuid()),
                preview.EntryDate,
                preview.CurrencyCode,
                JournalSourceType.OpeningBalance,
                preview.SourceReference,
                preview.Memo,
                _clock.UtcNow);

            foreach (var line in preview.Lines)
            {
                if (!line.LedgerAccountId.HasValue)
                {
                    return Result<JournalEntrySummaryResult>.Failure(ApplicationError.Validation(
                        nameof(command.Lines),
                        $"Opening balance line {line.LineNumber} does not resolve to a ledger account."));
                }

                var ledgerAccountId = LedgerAccountId.Create(line.LedgerAccountId.Value);

                if (line.Debit > 0)
                {
                    journalEntry.AddLine(JournalLine.DebitLine(
                        ledgerAccountId,
                        Money.Of(line.Debit, preview.CurrencyCode),
                        line.Description));
                }
                else
                {
                    journalEntry.AddLine(JournalLine.CreditLine(
                        ledgerAccountId,
                        Money.Of(line.Credit, preview.CurrencyCode),
                        line.Description));
                }
            }

            journalEntry.Post(_clock.UtcNow);

            await _journalEntries.AddAsync(journalEntry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<JournalEntrySummaryResult>.Success(ListJournalEntriesHandler.ToSummary(journalEntry));
        }
        catch (ArgumentException exception)
        {
            return Result<JournalEntrySummaryResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<JournalEntrySummaryResult>.Failure(ApplicationError.Validation(
                nameof(command.Lines),
                exception.Message));
        }
    }

    private async Task<ApplicationError?> ValidateUniqueSourceReferenceAsync(
        string sourceReference,
        CancellationToken cancellationToken)
    {
        var existingOpeningBalances = await _journalEntries.ListAsync(
            sourceType: JournalSourceType.OpeningBalance,
            cancellationToken: cancellationToken);

        return existingOpeningBalances.Any(entry =>
            entry.Status != JournalEntryStatus.Voided
            && string.Equals(entry.SourceReference, sourceReference, StringComparison.OrdinalIgnoreCase))
            ? ApplicationError.Conflict(
                nameof(PostOpeningBalanceImportCommand.SourceReference),
                $"Opening balance journal {sourceReference} already exists.")
            : null;
    }
}
