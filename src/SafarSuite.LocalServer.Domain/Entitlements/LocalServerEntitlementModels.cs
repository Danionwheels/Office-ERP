namespace SafarSuite.LocalServer.Domain.Entitlements;

public sealed record LocalServerCachedEntitlement(
    string BundleVersion,
    string Issuer,
    string Audience,
    Guid ClientId,
    string InstallationId,
    long EntitlementVersion,
    Guid BundleIssueId,
    Guid EntitlementSnapshotId,
    Guid ContractId,
    Guid SourceInvoiceId,
    string SourceInvoiceNumber,
    string Status,
    DateTimeOffset BundleIssuedAtUtc,
    DateTimeOffset EntitlementIssuedAtUtc,
    DateOnly ValidFrom,
    DateOnly PaidUntil,
    DateOnly WarningStartsAt,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    IReadOnlyCollection<LocalServerEntitlementModule> Modules,
    string PayloadJson,
    string SignatureAlgorithm,
    string SignatureKeyId,
    string PayloadSha256,
    string SignatureValue,
    DateTimeOffset ImportedAtUtc)
{
    public LocalServerEntitlementModule? FindModule(string moduleCode)
    {
        var cleanModuleCode = moduleCode.Trim();

        return Modules.FirstOrDefault(
            module => string.Equals(
                module.ModuleCode,
                cleanModuleCode,
                StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record LocalServerEntitlementModule(
    string ModuleCode,
    string Status,
    bool IsEnabled);

public sealed record LocalServerFeatureAccessDecision(
    string ModuleCode,
    bool IsAllowed,
    string AccessState,
    string Reason,
    long? EntitlementVersion,
    DateOnly? PaidUntil,
    DateOnly? WarningStartsAt,
    DateOnly? GraceUntil,
    DateOnly? OfflineValidUntil);

public sealed record LocalServerEntitlementStateDecision(
    bool IsAllowed,
    string AccessState,
    string Reason,
    long? EntitlementVersion,
    DateOnly? PaidUntil,
    DateOnly? WarningStartsAt,
    DateOnly? GraceUntil,
    DateOnly? OfflineValidUntil);

public static class LocalServerEntitlementAccessStates
{
    public const string Active = "Active";
    public const string Warning = "Warning";
    public const string Grace = "Grace";
    public const string Restricted = "Restricted";
    public const string Expired = "Expired";
    public const string Missing = "Missing";
    public const string NotYetValid = "NotYetValid";
    public const string ModuleDisabled = "ModuleDisabled";
    public const string InstallationMismatch = "InstallationMismatch";
    public const string StatusInactive = "StatusInactive";
}
