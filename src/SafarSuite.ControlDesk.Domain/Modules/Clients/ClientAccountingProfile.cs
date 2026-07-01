using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Clients;

public sealed class ClientAccountingProfile : Entity<ClientAccountingProfileId>
{
    private ClientAccountingProfile()
    {
        DefaultCurrencyCode = string.Empty;
    }

    private ClientAccountingProfile(
        ClientAccountingProfileId id,
        ClientId clientId,
        LedgerAccountId accountsReceivableAccountId,
        string defaultCurrencyCode,
        string? cloudCustomerId,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        ClientId = clientId;
        AccountsReceivableAccountId = accountsReceivableAccountId;
        DefaultCurrencyCode = defaultCurrencyCode;
        CloudCustomerId = cloudCustomerId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public ClientId ClientId { get; private set; }

    public LedgerAccountId AccountsReceivableAccountId { get; private set; }

    public string DefaultCurrencyCode { get; private set; }

    public string? CloudCustomerId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static ClientAccountingProfile Create(
        ClientAccountingProfileId id,
        ClientId clientId,
        LedgerAccountId accountsReceivableAccountId,
        string defaultCurrencyCode,
        string? cloudCustomerId,
        DateTimeOffset createdAtUtc)
    {
        return new ClientAccountingProfile(
            id,
            clientId,
            accountsReceivableAccountId,
            CleanCurrencyCode(defaultCurrencyCode),
            CleanCloudCustomerId(cloudCustomerId),
            createdAtUtc);
    }

    public void Update(
        LedgerAccountId accountsReceivableAccountId,
        string defaultCurrencyCode,
        string? cloudCustomerId,
        DateTimeOffset updatedAtUtc)
    {
        AccountsReceivableAccountId = accountsReceivableAccountId;
        DefaultCurrencyCode = CleanCurrencyCode(defaultCurrencyCode);
        CloudCustomerId = CleanCloudCustomerId(cloudCustomerId);
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string CleanCurrencyCode(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Default currency code is required.", nameof(currencyCode));
        }

        var cleanCurrencyCode = currencyCode.Trim().ToUpperInvariant();

        if (cleanCurrencyCode.Length != 3)
        {
            throw new ArgumentException(
                "Default currency code must be a three-letter ISO currency code.",
                nameof(currencyCode));
        }

        return cleanCurrencyCode;
    }

    private static string? CleanCloudCustomerId(string? cloudCustomerId)
    {
        return string.IsNullOrWhiteSpace(cloudCustomerId)
            ? null
            : cloudCustomerId.Trim();
    }
}
