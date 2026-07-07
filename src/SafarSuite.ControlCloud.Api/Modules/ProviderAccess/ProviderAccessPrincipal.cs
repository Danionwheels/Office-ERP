namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public sealed record ProviderAccessPrincipal(
    string Actor,
    string AuthenticationMethod,
    IReadOnlyCollection<string> Scopes,
    DateTimeOffset ExpiresAtUtc);
