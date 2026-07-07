using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public sealed class OpeningBalanceProfile : Entity<OpeningBalanceProfileId>
{
    private OpeningBalanceProfile()
    {
        CompanyCode = string.Empty;
    }

    private OpeningBalanceProfile(
        OpeningBalanceProfileId id,
        string companyCode,
        DateOnly fiscalYearFrom,
        DateOnly fiscalYearTo,
        OpeningBalanceProfileStatus status,
        bool transactionsAllowed,
        LedgerAccountId? profitAndLossCarryForwardAccountId,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        CompanyCode = CleanCompanyCode(companyCode);
        CreatedAtUtc = createdAtUtc;
        Configure(
            fiscalYearFrom,
            fiscalYearTo,
            status,
            transactionsAllowed,
            profitAndLossCarryForwardAccountId,
            createdAtUtc);
    }

    public string CompanyCode { get; private set; }

    public DateOnly FiscalYearFrom { get; private set; }

    public DateOnly FiscalYearTo { get; private set; }

    public OpeningBalanceProfileStatus Status { get; private set; }

    public bool TransactionsAllowed { get; private set; }

    public LedgerAccountId? ProfitAndLossCarryForwardAccountId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool IsConfigured => ProfitAndLossCarryForwardAccountId.HasValue;

    public static OpeningBalanceProfile Create(
        OpeningBalanceProfileId id,
        string companyCode,
        DateOnly fiscalYearFrom,
        DateOnly fiscalYearTo,
        OpeningBalanceProfileStatus status,
        bool transactionsAllowed,
        LedgerAccountId? profitAndLossCarryForwardAccountId,
        DateTimeOffset createdAtUtc)
    {
        return new OpeningBalanceProfile(
            id,
            companyCode,
            fiscalYearFrom,
            fiscalYearTo,
            status,
            transactionsAllowed,
            profitAndLossCarryForwardAccountId,
            createdAtUtc);
    }

    public void Configure(
        DateOnly fiscalYearFrom,
        DateOnly fiscalYearTo,
        OpeningBalanceProfileStatus status,
        bool transactionsAllowed,
        LedgerAccountId? profitAndLossCarryForwardAccountId,
        DateTimeOffset updatedAtUtc)
    {
        if (fiscalYearFrom == default)
        {
            throw new ArgumentException("Fiscal year start date is required.", nameof(fiscalYearFrom));
        }

        if (fiscalYearTo == default)
        {
            throw new ArgumentException("Fiscal year end date is required.", nameof(fiscalYearTo));
        }

        if (fiscalYearFrom > fiscalYearTo)
        {
            throw new ArgumentException("Fiscal year start date cannot be after the end date.");
        }

        FiscalYearFrom = fiscalYearFrom;
        FiscalYearTo = fiscalYearTo;
        Status = status;
        TransactionsAllowed = transactionsAllowed;
        ProfitAndLossCarryForwardAccountId = profitAndLossCarryForwardAccountId;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string CleanCompanyCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Company code is required.", nameof(value));
        }

        return value.Trim().ToUpperInvariant();
    }
}
