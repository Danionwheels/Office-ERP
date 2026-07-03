using System.Text.Json;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Common;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.AccountingSetup;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingPeriodCloseReadiness;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ListAccountingPeriods;

public sealed class ListAccountingPeriodsHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAccountingPeriodRepository _periods;

    public ListAccountingPeriodsHandler(IAccountingPeriodRepository periods)
    {
        _periods = periods;
    }

    public async Task<Result<ListAccountingPeriodsResult>> HandleAsync(
        ListAccountingPeriodsQuery query,
        CancellationToken cancellationToken = default)
    {
        var companyError = AccountingSetupDefaults.ValidateSingleCompanyCode(
            query.CompanyCode,
            nameof(query.CompanyCode));

        if (companyError is not null)
        {
            return Result<ListAccountingPeriodsResult>.Failure(companyError);
        }

        var companyCode = AccountingSetupDefaults.NormalizeCompanyCode(query.CompanyCode);
        var periods = await _periods.ListByCompanyAsync(
            companyCode,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        return Result<ListAccountingPeriodsResult>.Success(new ListAccountingPeriodsResult(
            companyCode,
            periods.Select(ToResult).ToArray()));
    }

    public static AccountingPeriodResult ToResult(AccountingPeriod period)
    {
        return new AccountingPeriodResult(
            period.Id.Value,
            period.CompanyCode,
            period.Name,
            period.StartsOn,
            period.EndsOn,
            period.Status.ToString(),
            period.CreatedAtUtc,
            period.UpdatedAtUtc,
            period.ClosedAtUtc,
            period.ReopenedAtUtc,
            ToArtifactResult(period.LatestCloseArtifact));
    }

    private static AccountingPeriodCloseArtifactResult? ToArtifactResult(
        AccountingPeriodCloseArtifact? artifact)
    {
        if (artifact is null)
        {
            return null;
        }

        var snapshot = TryDeserializeSnapshot(artifact.SnapshotJson);

        return new AccountingPeriodCloseArtifactResult(
            artifact.GeneratedAtUtc,
            artifact.GeneratedBy,
            artifact.CheckCount,
            artifact.BlockedCheckCount,
            artifact.CurrencyCount,
            artifact.PostedJournalCount,
            artifact.DraftJournalCount,
            snapshot?.Checks ?? Array.Empty<AccountingPeriodCloseReadinessCheckResult>(),
            snapshot?.Currencies ?? Array.Empty<AccountingPeriodCloseCurrencyResult>(),
            snapshot?.CloseJournalEntries ?? Array.Empty<AccountingPeriodCloseJournalArtifactResult>());
    }

    private static AccountingPeriodCloseArtifactSnapshot? TryDeserializeSnapshot(string snapshotJson)
    {
        try
        {
            return JsonSerializer.Deserialize<AccountingPeriodCloseArtifactSnapshot>(snapshotJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
