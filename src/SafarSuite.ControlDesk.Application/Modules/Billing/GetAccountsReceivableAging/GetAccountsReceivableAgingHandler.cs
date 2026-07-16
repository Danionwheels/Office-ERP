using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;
using SafarSuite.ControlDesk.Application.Modules.Billing.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Billing.GetAccountsReceivableAging;

public sealed class GetAccountsReceivableAgingHandler
{
    private readonly IBillingReportReader _reports;
    private readonly IClock _clock;

    public GetAccountsReceivableAgingHandler(IBillingReportReader reports, IClock clock)
    {
        _reports = reports;
        _clock = clock;
    }

    public async Task<Result<GetAccountsReceivableAgingResult>> HandleAsync(
        GetAccountsReceivableAgingQuery query,
        CancellationToken cancellationToken = default)
    {
        var today = _clock.Today;
        var asOfDate = query.AsOfDate ?? today;
        var currencyCode = string.IsNullOrWhiteSpace(query.CurrencyCode)
            ? "PKR"
            : query.CurrencyCode.Trim().ToUpperInvariant();

        if (asOfDate != today)
        {
            return Result<GetAccountsReceivableAgingResult>.Failure(ApplicationError.Validation(
                nameof(query.AsOfDate),
                "Accounts receivable aging is available for current operational balances only."));
        }

        if (!CurrencyCodeValidation.IsValid(currencyCode))
        {
            return Result<GetAccountsReceivableAgingResult>.Failure(ApplicationError.Validation(
                nameof(query.CurrencyCode),
                "Accounts receivable aging currency code must be three ASCII letters."));
        }

        var readModels = await _reports.ReadAccountsReceivableAgingAsync(
            new AccountsReceivableAgingReadRequest(asOfDate, currencyCode),
            cancellationToken);
        var clients = readModels
            .OrderBy(client => client.ClientName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(client => client.ClientCode, StringComparer.Ordinal)
            .ThenBy(client => client.ClientId)
            .Select(client => new AccountsReceivableAgingClientResult(
                client.ClientId,
                client.ClientCode,
                client.ClientName,
                client.CurrencyCode,
                client.CurrentAmount,
                client.Days1To30Amount,
                client.Days31To60Amount,
                client.Days61To90Amount,
                client.DaysOver90Amount,
                client.TotalOutstanding,
                client.InvoiceCount))
            .ToArray();
        var currency = new AccountsReceivableAgingCurrencyResult(
            currencyCode,
            clients.Sum(client => client.CurrentAmount),
            clients.Sum(client => client.Days1To30Amount),
            clients.Sum(client => client.Days31To60Amount),
            clients.Sum(client => client.Days61To90Amount),
            clients.Sum(client => client.DaysOver90Amount),
            clients.Sum(client => client.TotalOutstanding),
            clients.Sum(client => client.InvoiceCount),
            clients.LongLength);

        return Result<GetAccountsReceivableAgingResult>.Success(new GetAccountsReceivableAgingResult(
            asOfDate,
            [currency],
            clients));
    }
}
