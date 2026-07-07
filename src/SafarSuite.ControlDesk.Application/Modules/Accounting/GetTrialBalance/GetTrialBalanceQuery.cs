namespace SafarSuite.ControlDesk.Application.Modules.Accounting.GetTrialBalance;

public sealed record GetTrialBalanceQuery(
    DateOnly? FromDate,
    DateOnly? AsOfDate,
    string? CurrencyCode);
