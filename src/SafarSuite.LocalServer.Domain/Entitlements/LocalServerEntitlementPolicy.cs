namespace SafarSuite.LocalServer.Domain.Entitlements;

public sealed class LocalServerEntitlementPolicy
{
    public LocalServerEntitlementStateDecision EvaluateEntitlementState(
        LocalServerCachedEntitlement? entitlement,
        string expectedInstallationId,
        DateOnly asOfDate)
    {
        if (entitlement is null)
        {
            return DeniedState(
                LocalServerEntitlementAccessStates.Missing,
                "No signed entitlement bundle is cached.");
        }

        if (!string.Equals(
                entitlement.InstallationId,
                NormalizeRequiredText(expectedInstallationId, nameof(expectedInstallationId)),
                StringComparison.Ordinal))
        {
            return DeniedState(
                LocalServerEntitlementAccessStates.InstallationMismatch,
                "Cached entitlement belongs to a different installation.",
                entitlement);
        }

        if (!string.Equals(entitlement.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return DeniedState(
                LocalServerEntitlementAccessStates.StatusInactive,
                "Cached entitlement is not active.",
                entitlement);
        }

        if (asOfDate < entitlement.ValidFrom)
        {
            return DeniedState(
                LocalServerEntitlementAccessStates.NotYetValid,
                "Cached entitlement is not valid yet.",
                entitlement);
        }

        if (asOfDate <= entitlement.WarningStartsAt.AddDays(-1))
        {
            return AllowedState(
                LocalServerEntitlementAccessStates.Active,
                "Entitlement is active.",
                entitlement);
        }

        if (asOfDate <= entitlement.PaidUntil)
        {
            return AllowedState(
                LocalServerEntitlementAccessStates.Warning,
                "Entitlement is inside the renewal warning period.",
                entitlement);
        }

        if (asOfDate <= entitlement.GraceUntil)
        {
            return AllowedState(
                LocalServerEntitlementAccessStates.Grace,
                "Entitlement is inside grace period.",
                entitlement);
        }

        if (asOfDate <= entitlement.OfflineValidUntil)
        {
            return DeniedState(
                LocalServerEntitlementAccessStates.Restricted,
                "Entitlement is past grace period and should run restricted.",
                entitlement);
        }

        return DeniedState(
            LocalServerEntitlementAccessStates.Expired,
            "Cached entitlement is expired.",
            entitlement);
    }

    public LocalServerFeatureAccessDecision EvaluateFeatureAccess(
        LocalServerCachedEntitlement? entitlement,
        string expectedInstallationId,
        string moduleCode,
        DateOnly asOfDate)
    {
        var cleanModuleCode = NormalizeRequiredText(moduleCode, nameof(moduleCode));
        var entitlementState = EvaluateEntitlementState(
            entitlement,
            expectedInstallationId,
            asOfDate);

        if (entitlement is null)
        {
            return FromState(
                cleanModuleCode,
                entitlementState);
        }

        if (entitlementState.AccessState is
            LocalServerEntitlementAccessStates.InstallationMismatch or
            LocalServerEntitlementAccessStates.StatusInactive or
            LocalServerEntitlementAccessStates.NotYetValid)
        {
            return FromState(
                cleanModuleCode,
                entitlementState);
        }

        var module = entitlement.FindModule(cleanModuleCode);

        if (module is null || !module.IsEnabled)
        {
            return Denied(
                cleanModuleCode,
                LocalServerEntitlementAccessStates.ModuleDisabled,
                "Requested module is not enabled in the cached entitlement.",
                entitlement);
        }

        return FromState(
            cleanModuleCode,
            entitlementState);
    }

    private static LocalServerFeatureAccessDecision Allowed(
        string moduleCode,
        string accessState,
        string reason,
        LocalServerCachedEntitlement entitlement)
    {
        return CreateDecision(
            moduleCode,
            isAllowed: true,
            accessState,
            reason,
            entitlement);
    }

    private static LocalServerFeatureAccessDecision Denied(
        string moduleCode,
        string accessState,
        string reason,
        LocalServerCachedEntitlement? entitlement = null)
    {
        return CreateDecision(
            moduleCode,
            isAllowed: false,
            accessState,
            reason,
            entitlement);
    }

    private static LocalServerFeatureAccessDecision CreateDecision(
        string moduleCode,
        bool isAllowed,
        string accessState,
        string reason,
        LocalServerCachedEntitlement? entitlement)
    {
        return new LocalServerFeatureAccessDecision(
            moduleCode,
            isAllowed,
            accessState,
            reason,
            entitlement?.EntitlementVersion,
            entitlement?.PaidUntil,
            entitlement?.WarningStartsAt,
            entitlement?.GraceUntil,
            entitlement?.OfflineValidUntil);
    }

    private static LocalServerFeatureAccessDecision FromState(
        string moduleCode,
        LocalServerEntitlementStateDecision state)
    {
        return new LocalServerFeatureAccessDecision(
            moduleCode,
            state.IsAllowed,
            state.AccessState,
            state.Reason,
            state.EntitlementVersion,
            state.PaidUntil,
            state.WarningStartsAt,
            state.GraceUntil,
            state.OfflineValidUntil);
    }

    private static LocalServerEntitlementStateDecision AllowedState(
        string accessState,
        string reason,
        LocalServerCachedEntitlement entitlement)
    {
        return CreateStateDecision(
            isAllowed: true,
            accessState,
            reason,
            entitlement);
    }

    private static LocalServerEntitlementStateDecision DeniedState(
        string accessState,
        string reason,
        LocalServerCachedEntitlement? entitlement = null)
    {
        return CreateStateDecision(
            isAllowed: false,
            accessState,
            reason,
            entitlement);
    }

    private static LocalServerEntitlementStateDecision CreateStateDecision(
        bool isAllowed,
        string accessState,
        string reason,
        LocalServerCachedEntitlement? entitlement)
    {
        return new LocalServerEntitlementStateDecision(
            isAllowed,
            accessState,
            reason,
            entitlement?.EntitlementVersion,
            entitlement?.PaidUntil,
            entitlement?.WarningStartsAt,
            entitlement?.GraceUntil,
            entitlement?.OfflineValidUntil);
    }

    private static string NormalizeRequiredText(string value, string parameterName)
    {
        var normalized = value.Trim();

        if (normalized.Length == 0)
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return normalized;
    }
}
