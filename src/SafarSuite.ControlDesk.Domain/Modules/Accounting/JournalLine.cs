using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Accounting;

public sealed class JournalLine : ValueObject
{
    private JournalLine(
        LedgerAccountId ledgerAccountId,
        Money debit,
        Money credit,
        string? description)
    {
        LedgerAccountId = ledgerAccountId;
        Debit = debit;
        Credit = credit;
        Description = description;
    }

    public LedgerAccountId LedgerAccountId { get; }

    public Money Debit { get; }

    public Money Credit { get; }

    public string? Description { get; }

    public bool IsDebit => Debit.Amount > 0;

    public bool IsCredit => Credit.Amount > 0;

    public static JournalLine DebitLine(
        LedgerAccountId ledgerAccountId,
        Money amount,
        string? description = null)
    {
        EnsurePositive(amount);

        return new JournalLine(
            ledgerAccountId,
            amount,
            Money.Zero(amount.CurrencyCode),
            CleanDescription(description));
    }

    public static JournalLine CreditLine(
        LedgerAccountId ledgerAccountId,
        Money amount,
        string? description = null)
    {
        EnsurePositive(amount);

        return new JournalLine(
            ledgerAccountId,
            Money.Zero(amount.CurrencyCode),
            amount,
            CleanDescription(description));
    }

    private static void EnsurePositive(Money amount)
    {
        if (amount.Amount <= 0)
        {
            throw new ArgumentException("Journal line amount must be positive.", nameof(amount));
        }
    }

    private static string? CleanDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LedgerAccountId;
        yield return Debit;
        yield return Credit;
        yield return Description;
    }
}
