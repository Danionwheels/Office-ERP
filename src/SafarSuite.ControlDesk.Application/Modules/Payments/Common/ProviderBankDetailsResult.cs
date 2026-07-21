using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Common;

public sealed record ProviderBankDetailsResult(
    bool IsConfigured,
    string BankName,
    string AccountTitle,
    string AccountNumber,
    string Iban,
    string BranchOrRoutingInfo)
{
    public static ProviderBankDetailsResult From(ProviderBankDetails? details)
    {
        return details is null
            ? new ProviderBankDetailsResult(false, "", "", "", "", "")
            : new ProviderBankDetailsResult(
                details.IsConfigured,
                details.BankName,
                details.AccountTitle,
                details.AccountNumber,
                details.Iban,
                details.BranchOrRoutingInfo);
    }
}
