using System.Security.Cryptography;
using System.Text;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Api.Modules.LocalServer;

public sealed class LocalServerPairingOptions
{
    public const string SectionName = "LocalServer:Pairing";

    private const string DisplayNameEnvironmentVariable = "SAFARSUITE_LOCAL_PAIRING_DISPLAY_NAME";
    private const string HttpsUrlEnvironmentVariable = "SAFARSUITE_LOCAL_PAIRING_HTTPS_URL";
    private const string PairingModeEnvironmentVariable = "SAFARSUITE_LOCAL_PAIRING_MODE";
    private const string TlsCertificateSha256EnvironmentVariable = "SAFARSUITE_LOCAL_API_TLS_CERTIFICATE_SHA256";
    private const string TlsCaSha256EnvironmentVariable = "SAFARSUITE_LOCAL_API_TLS_CA_SHA256";
    private const string ServerPairingPublicKeyEnvironmentVariable = "SAFARSUITE_LOCAL_PAIRING_PUBLIC_KEY";
    private const string ServerPairingKeySha256EnvironmentVariable = "SAFARSUITE_LOCAL_PAIRING_KEY_SHA256";
    private const string RequestExpiresInHoursEnvironmentVariable = "SAFARSUITE_LOCAL_PAIRING_REQUEST_EXPIRES_HOURS";

    public string DisplayName { get; set; } = string.Empty;

    public string HttpsUrl { get; set; } = string.Empty;

    public string PairingMode { get; set; } = LocalServerPairingModes.ManagerApproval;

    public string TlsCertificateSha256 { get; set; } = string.Empty;

    public string TlsCaSha256 { get; set; } = string.Empty;

    public string ServerPairingPublicKey { get; set; } = string.Empty;

    public string ServerPairingKeySha256 { get; set; } = string.Empty;

    public int RequestExpiresInHours { get; set; } = 24;

    public static LocalServerPairingOptions FromConfiguration(IConfiguration configuration)
    {
        var options = configuration.GetSection(SectionName).Get<LocalServerPairingOptions>()
            ?? new LocalServerPairingOptions();

        ApplyEnvironmentOverride(configuration, DisplayNameEnvironmentVariable, value => options.DisplayName = value);
        ApplyEnvironmentOverride(configuration, HttpsUrlEnvironmentVariable, value => options.HttpsUrl = value);
        ApplyEnvironmentOverride(configuration, PairingModeEnvironmentVariable, value => options.PairingMode = value);
        ApplyEnvironmentOverride(configuration, TlsCertificateSha256EnvironmentVariable, value => options.TlsCertificateSha256 = value);
        ApplyEnvironmentOverride(configuration, TlsCaSha256EnvironmentVariable, value => options.TlsCaSha256 = value);
        ApplyEnvironmentOverride(configuration, ServerPairingPublicKeyEnvironmentVariable, value => options.ServerPairingPublicKey = value);
        ApplyEnvironmentOverride(configuration, ServerPairingKeySha256EnvironmentVariable, value => options.ServerPairingKeySha256 = value);
        ApplyEnvironmentOverride(configuration, RequestExpiresInHoursEnvironmentVariable, value =>
        {
            if (int.TryParse(value, out var parsed))
            {
                options.RequestExpiresInHours = parsed;
            }
        });

        options.DisplayName = NormalizeOptional(options.DisplayName);
        options.HttpsUrl = NormalizeOptional(options.HttpsUrl);
        options.PairingMode = NormalizePairingMode(options.PairingMode);
        options.TlsCertificateSha256 = NormalizeOptional(options.TlsCertificateSha256);
        options.TlsCaSha256 = NormalizeOptional(options.TlsCaSha256);
        options.ServerPairingPublicKey = NormalizeOptional(options.ServerPairingPublicKey);
        options.ServerPairingKeySha256 = NormalizeOptional(options.ServerPairingKeySha256);

        if (string.IsNullOrWhiteSpace(options.ServerPairingKeySha256)
            && !string.IsNullOrWhiteSpace(options.ServerPairingPublicKey))
        {
            options.ServerPairingKeySha256 = Sha256Hex(options.ServerPairingPublicKey);
        }

        if (options.RequestExpiresInHours <= 0)
        {
            options.RequestExpiresInHours = 24;
        }

        return options;
    }

    private static void ApplyEnvironmentOverride(
        IConfiguration configuration,
        string key,
        Action<string> apply)
    {
        var value = configuration[key]?.Trim();

        if (!string.IsNullOrWhiteSpace(value))
        {
            apply(value);
        }
    }

    private static string NormalizePairingMode(string? value)
    {
        var normalized = NormalizeOptional(value);

        return string.IsNullOrWhiteSpace(normalized)
            ? LocalServerPairingModes.ManagerApproval
            : normalized;
    }

    private static string NormalizeOptional(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string Sha256Hex(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
