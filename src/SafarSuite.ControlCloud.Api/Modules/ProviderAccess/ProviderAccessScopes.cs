namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public static class ProviderAccessScopes
{
    public const string Any = "*";
    public const string AppActivationRead = "app-activation:read";
    public const string AppActivationWrite = "app-activation:write";
    public const string ClientPortalManage = "client-portal:manage";
    public const string ProviderOperatorsManage = "provider-operators:manage";

    private static readonly string[] SupportedScopes =
    [
        Any,
        AppActivationRead,
        AppActivationWrite,
        ClientPortalManage,
        ProviderOperatorsManage
    ];

    public static IReadOnlyCollection<string> Supported => SupportedScopes;

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

    public static bool IsSupported(string? scope)
    {
        return !string.IsNullOrWhiteSpace(scope)
            && SupportedScopes.Contains(scope.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyCollection<string> FindUnsupported(IEnumerable<string>? scopes)
    {
        return (scopes ?? [])
            .Select(scope => scope.Trim())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Where(scope => !IsSupported(scope))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
