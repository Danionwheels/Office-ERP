namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ConfigureOpeningBalanceProfile;

public sealed record ConfigureOpeningBalanceProfileCommand(
    string? CompanyCode,
    DateOnly FiscalYearFrom,
    DateOnly FiscalYearTo,
    string Status,
    bool TransactionsAllowed,
    Guid? ProfitAndLossCarryForwardAccountId);
