using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Payments;

public sealed class ProviderBankDetails : Entity<ProviderBankDetailsId>
{
    private ProviderBankDetails()
    {
        BankName = string.Empty;
        AccountTitle = string.Empty;
        AccountNumber = string.Empty;
        Iban = string.Empty;
        BranchOrRoutingInfo = string.Empty;
    }

    private ProviderBankDetails(ProviderBankDetailsId id)
        : base(id)
    {
        BankName = string.Empty;
        AccountTitle = string.Empty;
        AccountNumber = string.Empty;
        Iban = string.Empty;
        BranchOrRoutingInfo = string.Empty;
    }

    public bool IsConfigured { get; private set; }

    public string BankName { get; private set; }

    public string AccountTitle { get; private set; }

    public string AccountNumber { get; private set; }

    public string Iban { get; private set; }

    public string BranchOrRoutingInfo { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static ProviderBankDetails CreateEmpty()
    {
        return new ProviderBankDetails(ProviderBankDetailsId.Singleton);
    }

    public void Update(
        string? bankName,
        string? accountTitle,
        string? accountNumber,
        string? iban,
        string? branchOrRoutingInfo,
        DateTimeOffset updatedAtUtc)
    {
        BankName = Clean(bankName, nameof(bankName), 160);
        AccountTitle = Clean(accountTitle, nameof(accountTitle), 160);
        AccountNumber = Clean(accountNumber, nameof(accountNumber), 100);
        Iban = Clean(iban, nameof(iban), 100).ToUpperInvariant();
        BranchOrRoutingInfo = Clean(branchOrRoutingInfo, nameof(branchOrRoutingInfo), 240);

        var isEmpty = BankName.Length == 0
            && AccountTitle.Length == 0
            && AccountNumber.Length == 0
            && Iban.Length == 0
            && BranchOrRoutingInfo.Length == 0;

        if (!isEmpty)
        {
            if (BankName.Length == 0)
            {
                throw new ArgumentException("Bank name is required when bank details are configured.", nameof(bankName));
            }

            if (AccountTitle.Length == 0)
            {
                throw new ArgumentException("Account title is required when bank details are configured.", nameof(accountTitle));
            }

            if (AccountNumber.Length == 0 && Iban.Length == 0)
            {
                throw new ArgumentException(
                    "Account number or IBAN is required when bank details are configured.",
                    nameof(accountNumber));
            }
        }

        IsConfigured = !isEmpty;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string Clean(string? value, string parameterName, int maximumLength)
    {
        var cleaned = value?.Trim() ?? string.Empty;

        if (cleaned.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return cleaned;
    }
}
