namespace SafarSuite.ControlDesk.Api.Modules.Auth;

public static class ControlDeskRoles
{
    public const string Administrator = "Administrator";
    public const string CommercialOperator = "CommercialOperator";
    public const string FinanceOperator = "FinanceOperator";
    public const string SupportOperator = "SupportOperator";
    public const string Auditor = "Auditor";
}

public static class ControlDeskScopes
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
}

public static class ControlDeskPolicies
{
    public const string CommandCenterRead = nameof(CommandCenterRead);
    public const string ClientsManage = nameof(ClientsManage);
    public const string ContractsManage = nameof(ContractsManage);
    public const string AccountingManage = nameof(AccountingManage);
    public const string BillingManage = nameof(BillingManage);
    public const string PaymentsManage = nameof(PaymentsManage);
    public const string EntitlementsManage = nameof(EntitlementsManage);
    public const string ControlCloudManage = nameof(ControlCloudManage);
    public const string DiagnosticsRead = nameof(DiagnosticsRead);
    public const string ReportsRead = nameof(ReportsRead);
}
