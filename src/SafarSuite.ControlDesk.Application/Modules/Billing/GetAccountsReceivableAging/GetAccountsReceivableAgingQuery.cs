namespace SafarSuite.ControlDesk.Application.Modules.Billing.GetAccountsReceivableAging;

public sealed record GetAccountsReceivableAgingQuery(
    DateOnly? AsOfDate,
    string? CurrencyCode);
