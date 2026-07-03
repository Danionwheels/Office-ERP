using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public sealed class AccountingPeriodCloseArtifact : ValueObject
{
    private AccountingPeriodCloseArtifact()
    {
        GeneratedBy = string.Empty;
        SnapshotJson = string.Empty;
    }

    private AccountingPeriodCloseArtifact(
        DateTimeOffset generatedAtUtc,
        string generatedBy,
        int checkCount,
        int blockedCheckCount,
        int currencyCount,
        int postedJournalCount,
        int draftJournalCount,
        string snapshotJson)
    {
        GeneratedAtUtc = generatedAtUtc;
        GeneratedBy = generatedBy;
        CheckCount = checkCount;
        BlockedCheckCount = blockedCheckCount;
        CurrencyCount = currencyCount;
        PostedJournalCount = postedJournalCount;
        DraftJournalCount = draftJournalCount;
        SnapshotJson = snapshotJson;
    }

    public DateTimeOffset GeneratedAtUtc { get; private set; }

    public string GeneratedBy { get; private set; }

    public int CheckCount { get; private set; }

    public int BlockedCheckCount { get; private set; }

    public int CurrencyCount { get; private set; }

    public int PostedJournalCount { get; private set; }

    public int DraftJournalCount { get; private set; }

    public string SnapshotJson { get; private set; }

    public static AccountingPeriodCloseArtifact Create(
        DateTimeOffset generatedAtUtc,
        string generatedBy,
        int checkCount,
        int blockedCheckCount,
        int currencyCount,
        int postedJournalCount,
        int draftJournalCount,
        string snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(generatedBy))
        {
            throw new ArgumentException("Close artifact actor is required.", nameof(generatedBy));
        }

        if (checkCount < 0)
        {
            throw new ArgumentException("Close artifact check count cannot be negative.", nameof(checkCount));
        }

        if (blockedCheckCount < 0)
        {
            throw new ArgumentException("Close artifact blocked check count cannot be negative.", nameof(blockedCheckCount));
        }

        if (currencyCount < 0)
        {
            throw new ArgumentException("Close artifact currency count cannot be negative.", nameof(currencyCount));
        }

        if (postedJournalCount < 0)
        {
            throw new ArgumentException("Close artifact posted journal count cannot be negative.", nameof(postedJournalCount));
        }

        if (draftJournalCount < 0)
        {
            throw new ArgumentException("Close artifact draft journal count cannot be negative.", nameof(draftJournalCount));
        }

        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            throw new ArgumentException("Close artifact snapshot is required.", nameof(snapshotJson));
        }

        return new AccountingPeriodCloseArtifact(
            generatedAtUtc,
            generatedBy.Trim(),
            checkCount,
            blockedCheckCount,
            currencyCount,
            postedJournalCount,
            draftJournalCount,
            snapshotJson);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return GeneratedAtUtc;
        yield return GeneratedBy;
        yield return CheckCount;
        yield return BlockedCheckCount;
        yield return CurrencyCount;
        yield return PostedJournalCount;
        yield return DraftJournalCount;
        yield return SnapshotJson;
    }
}
