namespace SafarSuite.ControlDesk.Application.Common.Validation;

public static class CurrencyCodeValidation
{
    public static bool IsValid(string? currencyCode)
    {
        return currencyCode is { Length: 3 }
            && currencyCode.All(character => character is >= 'A' and <= 'Z');
    }
}
