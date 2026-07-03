namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetTrialBalance;

public sealed record GetTrialBalanceQuery(
    DateOnly? AsOfDate,
    string? CurrencyCode);
