namespace SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

public sealed record ControlCloudProviderBankDetails(
    string BankName,
    string AccountTitle,
    string AccountNumber,
    string Iban,
    string BranchOrRoutingInfo,
    DateTimeOffset UpdatedAtUtc)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BankName)
        && !string.IsNullOrWhiteSpace(AccountTitle)
        && (!string.IsNullOrWhiteSpace(AccountNumber) || !string.IsNullOrWhiteSpace(Iban));
}
