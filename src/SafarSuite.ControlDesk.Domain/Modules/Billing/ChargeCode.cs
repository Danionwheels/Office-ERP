using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Billing;

public sealed class ChargeCode : Entity<ChargeCodeId>
{
    private ChargeCode(
        ChargeCodeId id,
        ChargeCodeKey code,
        string name,
        string? description,
        Money defaultUnitPrice,
        LedgerAccountId revenueAccountId,
        LedgerAccountId? taxAccountId,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Code = code;
        Name = name;
        Description = description;
        DefaultUnitPrice = defaultUnitPrice;
        RevenueAccountId = revenueAccountId;
        TaxAccountId = taxAccountId;
        CreatedAtUtc = createdAtUtc;
        Status = ChargeCodeStatus.Active;
    }

    public ChargeCodeKey Code { get; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public Money DefaultUnitPrice { get; private set; }

    public LedgerAccountId RevenueAccountId { get; private set; }

    public LedgerAccountId? TaxAccountId { get; private set; }

    public ChargeCodeStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static ChargeCode Create(
        ChargeCodeId id,
        ChargeCodeKey code,
        string name,
        string? description,
        Money defaultUnitPrice,
        LedgerAccountId revenueAccountId,
        LedgerAccountId? taxAccountId,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Charge code name is required.", nameof(name));
        }

        if (defaultUnitPrice.Amount < 0)
        {
            throw new ArgumentException("Default unit price cannot be negative.", nameof(defaultUnitPrice));
        }

        return new ChargeCode(
            id,
            code,
            name.Trim(),
            CleanText(description),
            defaultUnitPrice,
            revenueAccountId,
            taxAccountId,
            createdAtUtc);
    }

    public void Rename(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Charge code name is required.", nameof(name));
        }

        Name = name.Trim();
        Description = CleanText(description);
    }

    public void UpdateDefaultPrice(Money defaultUnitPrice)
    {
        if (defaultUnitPrice.Amount < 0)
        {
            throw new ArgumentException("Default unit price cannot be negative.", nameof(defaultUnitPrice));
        }

        DefaultUnitPrice = defaultUnitPrice;
    }

    public void SetPostingAccounts(LedgerAccountId revenueAccountId, LedgerAccountId? taxAccountId)
    {
        RevenueAccountId = revenueAccountId;
        TaxAccountId = taxAccountId;
    }

    public void Activate()
    {
        Status = ChargeCodeStatus.Active;
    }

    public void Deactivate()
    {
        Status = ChargeCodeStatus.Inactive;
    }

    private static string? CleanText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
