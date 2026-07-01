namespace SafarSuite.ControlDesk.Domain.SharedKernel;

public sealed class Money : ValueObject
{
    private Money()
    {
        CurrencyCode = string.Empty;
    }

    private Money(decimal amount, string currencyCode)
    {
        Amount = amount;
        CurrencyCode = currencyCode;
    }

    public decimal Amount { get; private set; }

    public string CurrencyCode { get; private set; }

    public static Money Of(decimal amount, string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }

        return new Money(decimal.Round(amount, 2), currencyCode.Trim().ToUpperInvariant());
    }

    public static Money Pkr(decimal amount)
    {
        return Of(amount, "PKR");
    }

    public static Money Zero(string currencyCode)
    {
        return Of(0m, currencyCode);
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);

        return Of(Amount + other.Amount, CurrencyCode);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);

        return Of(Amount - other.Amount, CurrencyCode);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(CurrencyCode, other.CurrencyCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Money values must use the same currency.");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return CurrencyCode;
    }
}
