using Microsoft.Extensions.Configuration;

namespace SafarSuite.ControlCloud.Api.Modules.ClientPortal;

public sealed class ClientPortalProviderAccessOptions
{
    public const string SectionName = "ClientPortal:ProviderAccess";

    public string SharedSecret { get; set; } =
        "local-development-provider-access-secret-change-before-cloud";

    public string SharedSecretFile { get; set; } = "";

    public string SessionSigningSecret { get; set; } =
        "local-development-provider-session-signing-secret-change-before-cloud";

    public string SessionSigningSecretFile { get; set; } = "";

    public string TotpProtectionSecret { get; set; } =
        "local-development-provider-totp-protection-secret-change-before-cloud";

    public string TotpProtectionSecretFile { get; set; } = "";

    public string ActiveSessionSigningKeyId { get; set; } = "";

    public ProviderAccessSessionSigningKeyOptions[] SessionSigningKeys { get; set; } = [];

    public int SessionMinutes { get; set; } = 60;

    public string[] DefaultScopes { get; set; } =
    [
        "app-activation:read",
        "app-activation:write",
        "client-portal:manage",
        "provider-operators:manage"
    ];

    public string OperatorStorePath { get; set; } = "App_Data/provider-access-operators.json";

    public ProviderAccessUserOptions[] Users { get; set; } =
    [
        new ProviderAccessUserOptions
        {
            UserId = "local-provider-admin",
            Email = "provider.admin@safarsuite.local",
            FullName = "Local Provider Admin",
            PasswordHash = "pbkdf2-sha256.120000.AQIDBAUGBwgJCgsMDQ4PEA.bKfX3l_4QOvv59HDi9Wq1UzY3FYjDWr3w5qQgkLufc4",
            Status = "Active",
            Scopes =
            [
                "app-activation:read",
                "app-activation:write",
                "client-portal:manage",
                "provider-operators:manage"
            ]
        }
    ];

    public static ClientPortalProviderAccessOptions FromConfiguration(
        IConfiguration configuration,
        string? contentRootPath = null)
    {
        var options = configuration.GetSection(SectionName).Get<ClientPortalProviderAccessOptions>()
            ?? new ClientPortalProviderAccessOptions();

        options.SharedSecret = ReadSecretOrInline(
            options.SharedSecret,
            options.SharedSecretFile,
            $"{SectionName}:SharedSecretFile",
            contentRootPath);
        options.SessionSigningSecret = ReadSecretOrInline(
            options.SessionSigningSecret,
            options.SessionSigningSecretFile,
            $"{SectionName}:SessionSigningSecretFile",
            contentRootPath);
        options.TotpProtectionSecret = ReadSecretOrInline(
            options.TotpProtectionSecret,
            options.TotpProtectionSecretFile,
            $"{SectionName}:TotpProtectionSecretFile",
            contentRootPath);

        for (var index = 0; index < options.SessionSigningKeys.Length; index++)
        {
            var key = options.SessionSigningKeys[index];
            key.Secret = ReadSecretOrInline(
                key.Secret,
                key.SecretFile,
                $"{SectionName}:SessionSigningKeys:{index}:SecretFile",
                contentRootPath);
        }

        return options;
    }

    private static string ReadSecretOrInline(
        string inlineSecret,
        string secretFile,
        string optionName,
        string? contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(secretFile))
        {
            return inlineSecret;
        }

        var secretPath = ResolveSecretPath(secretFile, contentRootPath);

        if (!File.Exists(secretPath))
        {
            throw new InvalidOperationException(
                $"{optionName} points to a secret file that does not exist: {secretPath}");
        }

        var secret = File.ReadAllText(secretPath).Trim();

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                $"{optionName} points to an empty secret file: {secretPath}");
        }

        return secret;
    }

    private static string ResolveSecretPath(string secretFile, string? contentRootPath)
    {
        var trimmed = secretFile.Trim();

        return Path.IsPathRooted(trimmed)
            ? Path.GetFullPath(trimmed)
            : Path.GetFullPath(Path.Combine(
                string.IsNullOrWhiteSpace(contentRootPath)
                    ? Directory.GetCurrentDirectory()
                    : contentRootPath,
                trimmed));
    }
}

public sealed class ProviderAccessSessionSigningKeyOptions
{
    public string KeyId { get; set; } = "";

    public string Secret { get; set; } = "";

    public string SecretFile { get; set; } = "";
}

public sealed class ProviderAccessUserOptions
{
    public string UserId { get; set; } = "";

    public string Email { get; set; } = "";

    public string FullName { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Status { get; set; } = "Active";

    public string[] Scopes { get; set; } = [];
}
