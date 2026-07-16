namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudProviderBankDetailsEntity
{
    public string BankDetailsId { get; set; } = "provider";
    public string BankName { get; set; } = "";
    public string AccountTitle { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string Iban { get; set; } = "";
    public string BranchOrRoutingInfo { get; set; } = "";
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
