using System.Text.Json;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseJournalPreview;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseReadiness;
using SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountingPeriods;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.CloseAccountingPeriod;

public sealed class CloseAccountingPeriodHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAccountingPeriodRepository _periods;
    private readonly AccountingPeriodCloseReadinessService _readinessService;
    private readonly GetAccountingPeriodCloseJournalPreviewHandler _closeJournalPreview;
    private readonly IJournalEntryRepository _journalEntries;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public CloseAccountingPeriodHandler(
        IAccountingPeriodRepository periods,
        AccountingPeriodCloseReadinessService readinessService,
        GetAccountingPeriodCloseJournalPreviewHandler closeJournalPreview,
        IJournalEntryRepository journalEntries,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _periods = periods;
        _readinessService = readinessService;
        _closeJournalPreview = closeJournalPreview;
        _journalEntries = journalEntries;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<Result<AccountingPeriodResult>> HandleAsync(
        CloseAccountingPeriodCommand command,
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

        var readiness = await _readinessService.CheckAsync(command.AccountingPeriodId, cancellationToken);

        if (readiness.IsFailure)
        {
            return Result<AccountingPeriodResult>.Failure(readiness.Errors);
        }

        if (!readiness.Value.CanClose)
        {
            var blockedCheck = readiness.Value.Checks.First(check => check.Status == "Blocked");

            return Result<AccountingPeriodResult>.Failure(ApplicationError.Conflict(
                blockedCheck.Target ?? nameof(command.AccountingPeriodId),
                blockedCheck.Message));
        }

        var closedAtUtc = _clock.UtcNow;
        var closeJournalPreview = await _closeJournalPreview.HandleAsync(
            new GetAccountingPeriodCloseJournalPreviewQuery(command.AccountingPeriodId),
            cancellationToken);

        if (closeJournalPreview.IsFailure)
        {
            return Result<AccountingPeriodResult>.Failure(closeJournalPreview.Errors);
        }

        if (!closeJournalPreview.Value.CanGenerate)
        {
            return Result<AccountingPeriodResult>.Failure(ApplicationError.Conflict(
                "CloseJournalPreview",
                closeJournalPreview.Value.Blockers.FirstOrDefault()
                    ?? "Close journal preview is not ready."));
        }

        var closeJournalEntries = await PostCloseJournalEntriesAsync(
            closeJournalPreview.Value,
            closedAtUtc,
            cancellationToken);

        if (closeJournalEntries.IsFailure)
        {
            return Result<AccountingPeriodResult>.Failure(closeJournalEntries.Errors);
        }

        var closeArtifact = CreateCloseArtifact(
            readiness.Value,
            closeJournalEntries.Value,
            closedAtUtc,
            command.ClosedBy);

        period.Close(closedAtUtc, closeArtifact);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AccountingPeriodResult>.Success(ListAccountingPeriodsHandler.ToResult(period));
    }

    private async Task<Result<IReadOnlyCollection<AccountingPeriodCloseJournalArtifactResult>>> PostCloseJournalEntriesAsync(
        GetAccountingPeriodCloseJournalPreviewResult preview,
        DateTimeOffset postedAtUtc,
        CancellationToken cancellationToken)
    {
        var postedEntries = new List<AccountingPeriodCloseJournalArtifactResult>();

        try
        {
            foreach (var previewEntry in preview.Entries.Where(entry => entry.Lines.Count > 0))
            {
                var journalEntry = JournalEntry.Create(
                    JournalEntryId.Create(_idGenerator.NewGuid()),
                    previewEntry.EntryDate,
                    previewEntry.CurrencyCode,
                    JournalSourceType.PeriodClose,
                    previewEntry.SourceReference,
                    previewEntry.Memo,
                    postedAtUtc);

                foreach (var line in previewEntry.Lines)
                {
                    var ledgerAccountId = LedgerAccountId.Create(line.LedgerAccountId);

                    if (line.Debit > 0)
                    {
                        journalEntry.AddLine(JournalLine.DebitLine(
                            ledgerAccountId,
                            Money.Of(line.Debit, previewEntry.CurrencyCode),
                            line.Description));
                    }
                    else if (line.Credit > 0)
                    {
                        journalEntry.AddLine(JournalLine.CreditLine(
                            ledgerAccountId,
                            Money.Of(line.Credit, previewEntry.CurrencyCode),
                            line.Description));
                    }
                }

                journalEntry.Post(postedAtUtc);
                await _journalEntries.AddAsync(journalEntry, cancellationToken);

                postedEntries.Add(new AccountingPeriodCloseJournalArtifactResult(
                    journalEntry.Id.Value,
                    journalEntry.SourceReference ?? string.Empty,
                    journalEntry.Memo ?? string.Empty,
                    journalEntry.EntryDate,
                    journalEntry.CurrencyCode,
                    journalEntry.TotalDebit.Amount,
                    journalEntry.TotalCredit.Amount));
            }

            return Result<IReadOnlyCollection<AccountingPeriodCloseJournalArtifactResult>>.Success(postedEntries);
        }
        catch (ArgumentException exception)
        {
            return Result<IReadOnlyCollection<AccountingPeriodCloseJournalArtifactResult>>.Failure(
                ApplicationError.Validation(exception.ParamName ?? nameof(preview), exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<IReadOnlyCollection<AccountingPeriodCloseJournalArtifactResult>>.Failure(
                ApplicationError.Validation(nameof(preview.Entries), exception.Message));
        }
    }

    private static AccountingPeriodCloseArtifact CreateCloseArtifact(
        GetAccountingPeriodCloseReadinessResult readiness,
        IReadOnlyCollection<AccountingPeriodCloseJournalArtifactResult> closeJournalEntries,
        DateTimeOffset generatedAtUtc,
        string? closedBy)
    {
        var generatedBy = NormalizeActor(closedBy);
        var closeJournalEntryList = closeJournalEntries.ToArray();
        var snapshot = new AccountingPeriodCloseArtifactSnapshot(
            2,
            generatedAtUtc,
            generatedBy,
            new AccountingPeriodCloseArtifactPeriodSnapshot(
                readiness.Period.AccountingPeriodId,
                readiness.Period.CompanyCode,
                readiness.Period.Name,
                readiness.Period.StartsOn,
                readiness.Period.EndsOn,
            readiness.Period.Status),
            readiness.CanClose,
            readiness.Checks,
            readiness.Currencies)
        {
            CloseJournalEntries = closeJournalEntryList
        };
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);

        return AccountingPeriodCloseArtifact.Create(
            generatedAtUtc,
            generatedBy,
            readiness.Checks.Count,
            readiness.Checks.Count(check => check.Status == "Blocked"),
            readiness.Currencies.Count,
            readiness.Currencies.Sum(currency => currency.PostedJournalCount) + closeJournalEntryList.Length,
            readiness.Currencies.Sum(currency => currency.DraftJournalCount),
            snapshotJson);
    }

    private static string NormalizeActor(string? closedBy)
    {
        var actor = closedBy?.Trim();

        return string.IsNullOrWhiteSpace(actor) ? "control-desk" : actor;
    }
}
