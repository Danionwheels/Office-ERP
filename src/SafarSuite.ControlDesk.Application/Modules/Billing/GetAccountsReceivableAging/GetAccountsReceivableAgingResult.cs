namespace SafarSuite.ControlDesk.Application.Modules.Billing.GetAccountsReceivableAging;

public sealed record GetAccountsReceivableAgingResult(
    DateOnly AsOfDate,
    IReadOnlyCollection<AccountsReceivableAgingCurrencyResult> Currencies,
    IReadOnlyCollection<AccountsReceivableAgingClientResult> Clients);

public sealed record AccountsReceivableAgingCurrencyResult(
    string CurrencyCode,
    decimal CurrentAmount,
    decimal Days1To30Amount,
    decimal Days31To60Amount,
    decimal Days61To90Amount,
    decimal DaysOver90Amount,
    decimal TotalOutstanding,
    long InvoiceCount,
    long ClientCount);

public sealed record AccountsReceivableAgingClientResult(
    Guid ClientId,
    string ClientCode,
    string ClientName,
    string CurrencyCode,
    decimal CurrentAmount,
    decimal Days1To30Amount,
    decimal Days31To60Amount,
    decimal Days61To90Amount,
    decimal DaysOver90Amount,
    decimal TotalOutstanding,
    long InvoiceCount);
