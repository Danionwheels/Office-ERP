namespace SafarSuite.ControlDesk.Domain.Modules.Auth;

public static class LocalOperatorScope
{
    public const string Admin = "control-desk:admin";
    public const string CommandCenterRead = "command-center:read";
    public const string ClientsManage = "clients:manage";
    public const string ContractsManage = "contracts:manage";
    public const string AccountingManage = "accounting:manage";
    public const string BillingManage = "billing:manage";
    public const string PaymentsManage = "payments:manage";
    public const string EntitlementsManage = "entitlements:manage";
    public const string ControlCloudManage = "control-cloud:manage";
    public const string DiagnosticsRead = "diagnostics:read";
    public const string ReportsRead = "reports:read";

    private static readonly string[] AllowedValues =
    [
        Admin,
        CommandCenterRead,
        ClientsManage,
        ContractsManage,
        AccountingManage,
        BillingManage,
        PaymentsManage,
        EntitlementsManage,
        ControlCloudManage,
        DiagnosticsRead,
        ReportsRead
    ];

    public static string Normalize(string value)
    {
        var cleaned = value?.Trim() ?? string.Empty;
        var canonical = AllowedValues.FirstOrDefault(candidate =>
            string.Equals(candidate, cleaned, StringComparison.OrdinalIgnoreCase));

        return canonical
            ?? throw new ArgumentException($"Unknown local operator scope '{cleaned}'.", nameof(value));
    }
}
