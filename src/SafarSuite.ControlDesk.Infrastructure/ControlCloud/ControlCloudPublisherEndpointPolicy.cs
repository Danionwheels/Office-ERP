namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public static class ControlCloudPublisherEndpointPolicy
{
    private const string DevelopmentSigningSecret =
        "local-development-signing-secret-change-before-cloud";

    public static bool IsConfigured(
        ControlCloudPublisherOptions options,
        bool requireHttps)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.Mode.Equals("Http", StringComparison.OrdinalIgnoreCase)
               && TryResolveEndpoint(options, requireHttps, out _);
    }

    public static bool TryResolveEndpoint(
        ControlCloudPublisherOptions options,
        bool requireHttps,
        out Uri endpointUri)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!Uri.TryCreate(options.EndpointUrl, UriKind.Absolute, out var resolvedEndpoint)
            || (!resolvedEndpoint.Scheme.Equals(
                    Uri.UriSchemeHttp,
                    StringComparison.OrdinalIgnoreCase)
                && !resolvedEndpoint.Scheme.Equals(
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase))
            || (requireHttps
                && !resolvedEndpoint.Scheme.Equals(
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase)))
        {
            endpointUri = null!;
            return false;
        }

        endpointUri = resolvedEndpoint;
        return true;
    }

    public static bool HasValidEnvelopeConfiguration(
        ControlCloudPublisherOptions options,
        bool allowDevelopmentValues)
    {
        ArgumentNullException.ThrowIfNull(options);

        var sourceSystem = options.SourceSystem?.Trim() ?? string.Empty;
        var environment = options.Environment?.Trim() ?? string.Empty;
        var signingKeyId = options.SigningKeyId?.Trim() ?? string.Empty;
        var signingSecret = options.SigningSecret?.Trim() ?? string.Empty;

        if (sourceSystem.Length == 0
            || environment.Length == 0
            || signingKeyId.Length == 0
            || signingSecret.Length < 32)
        {
            return false;
        }

        return allowDevelopmentValues
               || (!environment.Equals("Local", StringComparison.OrdinalIgnoreCase)
                   && !signingKeyId.Equals("local-dev", StringComparison.OrdinalIgnoreCase)
                   && !signingSecret.Equals(
                       DevelopmentSigningSecret,
                       StringComparison.Ordinal));
    }
}
