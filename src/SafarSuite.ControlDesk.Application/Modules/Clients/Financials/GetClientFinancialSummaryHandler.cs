using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.Financials;

public sealed record GetClientFinancialSummaryQuery(
    Guid ClientId,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null);

public sealed class GetClientFinancialSummaryHandler
{
    private readonly IClientFinancialReader _financials;

    public GetClientFinancialSummaryHandler(IClientFinancialReader financials)
    {
        _financials = financials;
    }

    public async Task<Result<ClientFinancialSummaryResult>> HandleAsync(
        GetClientFinancialSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationError = ClientFinancialQueryRules.ValidateClientAndDates(
            query.ClientId,
            query.FromDate,
            query.ToDate);

        if (validationError is not null)
        {
            return Result<ClientFinancialSummaryResult>.Failure(validationError);
        }

        var clientId = ClientId.Create(query.ClientId);

        if (!await _financials.ClientExistsAsync(clientId, cancellationToken))
        {
            return Result<ClientFinancialSummaryResult>.Failure(ApplicationError.NotFound(
                nameof(query.ClientId),
                "Client was not found."));
        }

        var summary = await _financials.ReadSummaryAsync(
            clientId,
            query.FromDate,
            query.ToDate,
            cancellationToken);

        return Result<ClientFinancialSummaryResult>.Success(new ClientFinancialSummaryResult(
            query.ClientId,
            query.FromDate,
            query.ToDate,
            summary.Currencies.Select(currency => new ClientFinancialCurrencySummaryResult(
                currency.CurrencyCode,
                currency.TotalInvoiced,
                currency.TotalPaid,
                currency.AvailableCredit,
                currency.BalanceDue,
                currency.InvoiceCount,
                currency.OpenInvoiceCount)).ToArray()));
    }
}
