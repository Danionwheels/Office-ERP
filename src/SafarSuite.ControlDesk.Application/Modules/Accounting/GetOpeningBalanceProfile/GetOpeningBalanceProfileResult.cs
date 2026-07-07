namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetOpeningBalanceProfile;

public sealed record GetOpeningBalanceProfileResult(
    string CompanyCode,
    DateOnly FiscalYearFrom,
    DateOnly FiscalYearTo,
    string Status,
    bool TransactionsAllowed,
    Guid? ProfitAndLossCarryForwardAccountId,
    OpeningBalanceProfileAccountResult? ProfitAndLossCarryForwardAccount,
    bool IsConfigured,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record OpeningBalanceProfileAccountResult(
    Guid LedgerAccountId,
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    string Status);
