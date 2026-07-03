using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public sealed class AccountingPeriod : Entity<AccountingPeriodId>
{
    private readonly List<AccountingPeriodCloseArtifact> _closeArtifacts = [];

    private AccountingPeriod()
    {
        CompanyCode = string.Empty;
        Name = string.Empty;
    }

    private AccountingPeriod(
        AccountingPeriodId id,
        string companyCode,
        string name,
        DateOnly startsOn,
        DateOnly endsOn,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        CompanyCode = companyCode;
        Name = name;
        StartsOn = startsOn;
        EndsOn = endsOn;
        Status = AccountingPeriodStatus.Open;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public string CompanyCode { get; private set; }

    public string Name { get; private set; }

    public DateOnly StartsOn { get; private set; }

    public DateOnly EndsOn { get; private set; }

    public AccountingPeriodStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public DateTimeOffset? ReopenedAtUtc { get; private set; }

    public IReadOnlyCollection<AccountingPeriodCloseArtifact> CloseArtifacts => _closeArtifacts.AsReadOnly();

    public AccountingPeriodCloseArtifact? LatestCloseArtifact => _closeArtifacts
        .OrderByDescending(artifact => artifact.GeneratedAtUtc)
        .FirstOrDefault();

    public static AccountingPeriod Create(
        AccountingPeriodId id,
        string companyCode,
        string name,
        DateOnly startsOn,
        DateOnly endsOn,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(companyCode))
        {
            throw new ArgumentException("Company code is required.", nameof(companyCode));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Accounting period name is required.", nameof(name));
        }

        if (endsOn < startsOn)
        {
            throw new ArgumentException("Accounting period end date cannot be before start date.", nameof(endsOn));
        }

        return new AccountingPeriod(
            id,
            companyCode.Trim().ToUpperInvariant(),
            name.Trim(),
            startsOn,
            endsOn,
            createdAtUtc);
    }

    public bool Contains(DateOnly date)
    {
        return date >= StartsOn && date <= EndsOn;
    }

    public void Close(DateTimeOffset closedAtUtc, AccountingPeriodCloseArtifact closeArtifact)
    {
        if (Status == AccountingPeriodStatus.Closed)
        {
            return;
        }

        Status = AccountingPeriodStatus.Closed;
        ClosedAtUtc = closedAtUtc;
        UpdatedAtUtc = closedAtUtc;
        _closeArtifacts.Add(closeArtifact);
    }

    public void Reopen(DateTimeOffset reopenedAtUtc)
    {
        if (Status == AccountingPeriodStatus.Open)
        {
            return;
        }

        Status = AccountingPeriodStatus.Open;
        ReopenedAtUtc = reopenedAtUtc;
        UpdatedAtUtc = reopenedAtUtc;
    }
}
