using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public sealed class AccountingControlSettings : Entity<AccountingControlSettingsId>
{
    private AccountingControlSettings()
    {
        CompanyCode = string.Empty;
        BaseCurrencyCode = string.Empty;
    }

    private AccountingControlSettings(
        AccountingControlSettingsId id,
        string companyCode,
        string baseCurrencyCode,
        LedgerAccountId? retainedEarningsAccountId,
        LedgerAccountId? incomeSummaryAccountId,
        LedgerAccountId? roundingAccountId,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        CompanyCode = CleanCompanyCode(companyCode);
        BaseCurrencyCode = string.Empty;
        CreatedAtUtc = createdAtUtc;
        Configure(
            baseCurrencyCode,
            retainedEarningsAccountId,
            incomeSummaryAccountId,
            roundingAccountId,
            createdAtUtc);
    }

    public string CompanyCode { get; private set; }

    public string BaseCurrencyCode { get; private set; }

    public LedgerAccountId? RetainedEarningsAccountId { get; private set; }

    public LedgerAccountId? IncomeSummaryAccountId { get; private set; }

    public LedgerAccountId? RoundingAccountId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool IsConfigured =>
        RetainedEarningsAccountId.HasValue
        && IncomeSummaryAccountId.HasValue
        && RoundingAccountId.HasValue;

    public static AccountingControlSettings Create(
        AccountingControlSettingsId id,
        string companyCode,
        string baseCurrencyCode,
        LedgerAccountId? retainedEarningsAccountId,
        LedgerAccountId? incomeSummaryAccountId,
        LedgerAccountId? roundingAccountId,
        DateTimeOffset createdAtUtc)
    {
        return new AccountingControlSettings(
            id,
            companyCode,
            baseCurrencyCode,
            retainedEarningsAccountId,
            incomeSummaryAccountId,
            roundingAccountId,
            createdAtUtc);
    }

    public void Configure(
        string baseCurrencyCode,
        LedgerAccountId? retainedEarningsAccountId,
        LedgerAccountId? incomeSummaryAccountId,
        LedgerAccountId? roundingAccountId,
        DateTimeOffset updatedAtUtc)
    {
        BaseCurrencyCode = CleanCurrencyCode(baseCurrencyCode);
        RetainedEarningsAccountId = retainedEarningsAccountId;
        IncomeSummaryAccountId = incomeSummaryAccountId;
        RoundingAccountId = roundingAccountId;
        UpdatedAtUtc = updatedAtUtc;

        EnsureDistinctControlAccounts();
    }

    private void EnsureDistinctControlAccounts()
    {
        var accounts = new[]
            {
                RetainedEarningsAccountId,
                IncomeSummaryAccountId,
                RoundingAccountId
            }
            .Where(accountId => accountId.HasValue)
            .Select(accountId => accountId!.Value.Value)
            .ToArray();

        if (accounts.Distinct().Count() != accounts.Length)
        {
            throw new ArgumentException("Accounting control accounts must be distinct.");
        }
    }

    private static string CleanCompanyCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Company code is required.", nameof(value));
        }

        return value.Trim().ToUpperInvariant();
    }

    private static string CleanCurrencyCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Base currency code is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length != 3 || !normalized.All(char.IsLetter))
        {
            throw new ArgumentException("Base currency code must be a three-letter ISO code.", nameof(value));
        }

        return normalized;
    }
}
