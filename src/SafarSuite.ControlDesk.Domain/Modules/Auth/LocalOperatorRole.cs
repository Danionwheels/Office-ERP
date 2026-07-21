namespace SafarSuite.ControlDesk.Domain.Modules.Auth;

public static class LocalOperatorRole
{
    public const string Administrator = "Administrator";
    public const string CommercialOperator = "CommercialOperator";
    public const string FinanceOperator = "FinanceOperator";
    public const string SupportOperator = "SupportOperator";
    public const string Auditor = "Auditor";

    private static readonly string[] AllowedValues =
    [
        Administrator,
        CommercialOperator,
        FinanceOperator,
        SupportOperator,
        Auditor
    ];

    public static string Normalize(string value)
    {
        var cleaned = value?.Trim() ?? string.Empty;
        var canonical = AllowedValues.FirstOrDefault(candidate =>
            string.Equals(candidate, cleaned, StringComparison.OrdinalIgnoreCase));

        return canonical
            ?? throw new ArgumentException($"Unknown local operator role '{cleaned}'.", nameof(value));
    }
}
