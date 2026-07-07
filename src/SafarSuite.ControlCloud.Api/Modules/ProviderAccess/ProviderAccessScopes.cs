namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public static class ProviderAccessScopes
{
    public const string Any = "*";
    public const string AppActivationRead = "app-activation:read";
    public const string AppActivationWrite = "app-activation:write";
    public const string ClientPortalManage = "client-portal:manage";

    public static IReadOnlyCollection<string> Normalize(
        IEnumerable<string>? scopes,
        IEnumerable<string> defaultScopes)
    {
        var normalized = (scopes ?? defaultScopes)
            .Select(scope => scope.Trim())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized.Length == 0
            ? defaultScopes
                .Select(scope => scope.Trim())
                .Where(scope => !string.IsNullOrWhiteSpace(scope))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : normalized;
    }
}
