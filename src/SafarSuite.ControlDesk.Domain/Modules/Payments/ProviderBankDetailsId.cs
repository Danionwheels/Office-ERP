namespace SafarSuite.ControlDesk.Domain.Modules.Payments;

public readonly record struct ProviderBankDetailsId(Guid Value)
{
    public static readonly ProviderBankDetailsId Singleton = new(
        Guid.Parse("9d4793f8-f402-44f4-a73a-8ce0f75d5fc2"));

    public static ProviderBankDetailsId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Provider bank details id cannot be empty.", nameof(value));
        }

        return new ProviderBankDetailsId(value);
    }
}
