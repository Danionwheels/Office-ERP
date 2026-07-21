using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

namespace SafarSuite.ControlDesk.Api.Modules.ControlCloud;

internal sealed class HttpContextControlCloudProviderAccessCredentialSource
    : IControlCloudProviderAccessCredentialSource
{
    public const string ProviderAccessTokenOverrideHeaderName =
        "X-SafarSuite-Provider-Access-Token";

    private const string ProviderAccessHeaderName = "X-SafarSuite-Provider-Key";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextControlCloudProviderAccessCredentialSource(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool TryGetCredential(
        string configuredToken,
        string configuredSecret,
        out ControlCloudProviderAccessCredential credential)
    {
        var overrideToken = GetOverrideToken();

        if (!string.IsNullOrWhiteSpace(overrideToken))
        {
            credential = new ControlCloudProviderAccessCredential(
                "Authorization",
                $"Bearer {NormalizeBearerToken(overrideToken)}");

            return true;
        }

        var providerAccessToken = configuredToken.Trim();

        if (!string.IsNullOrWhiteSpace(providerAccessToken))
        {
            credential = new ControlCloudProviderAccessCredential(
                "Authorization",
                $"Bearer {NormalizeBearerToken(providerAccessToken)}");

            return true;
        }

        var providerAccessSecret = configuredSecret.Trim();

        if (!string.IsNullOrWhiteSpace(providerAccessSecret))
        {
            credential = new ControlCloudProviderAccessCredential(
                ProviderAccessHeaderName,
                providerAccessSecret);

            return true;
        }

        credential = new ControlCloudProviderAccessCredential("", "");
        return false;
    }

    public bool HasCredential(string configuredToken, string configuredSecret)
    {
        return !string.IsNullOrWhiteSpace(GetOverrideToken())
            || !string.IsNullOrWhiteSpace(configuredToken)
            || !string.IsNullOrWhiteSpace(configuredSecret);
    }

    private string GetOverrideToken()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers[
                ProviderAccessTokenOverrideHeaderName]
            .FirstOrDefault()
            ?.Trim()
            ?? "";
    }

    private static string NormalizeBearerToken(string token)
    {
        const string bearerPrefix = "Bearer ";

        return token.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? token[bearerPrefix.Length..].Trim()
            : token.Trim();
    }
}
