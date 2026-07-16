using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Common;

public sealed class ClientCreditBalanceService
{
    private readonly IClientFinancialReader _financials;

    public ClientCreditBalanceService(IClientFinancialReader financials)
    {
        _financials = financials;
    }

    public async Task<ClientCreditBalance> CalculateAsync(
        ClientId clientId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        var balance = await _financials.ReadCreditBalanceAsync(
            clientId,
            currencyCode,
            cancellationToken);

        return new ClientCreditBalance(
            balance.CurrencyCode,
            balance.InvoiceBalance,
            balance.CreditNoteAmount,
            balance.RefundAmount,
            balance.AppliedCreditAmount);
    }
}
