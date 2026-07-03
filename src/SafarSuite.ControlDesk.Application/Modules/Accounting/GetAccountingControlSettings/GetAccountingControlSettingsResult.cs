namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetAccountingControlSettings;

public sealed record GetAccountingControlSettingsResult(
    string CompanyCode,
    string BaseCurrencyCode,
    Guid? RetainedEarningsAccountId,
    AccountingControlAccountResult? RetainedEarningsAccount,
    Guid? IncomeSummaryAccountId,
    AccountingControlAccountResult? IncomeSummaryAccount,
    Guid? RoundingAccountId,
    AccountingControlAccountResult? RoundingAccount,
    bool IsConfigured,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record AccountingControlAccountResult(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    string Status);
