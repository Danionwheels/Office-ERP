using System.Net.Mail;
using System.Text;

namespace SafarSuite.StagingPreflight;

public sealed class StagingPreflightValidator
{
    private const long MaximumSecretFileBytes = 1_048_576;
    private static readonly string[] PlaceholderMarkers =
    [
        "replace-with",
        "must-match",
        "change-before",
        "local-development",
        "placeholder",
        "change-me",
        "changeme",
        "example.com",
        "${",
        "<password>",
        "<secret>",
        "<server>"
    ];

    private static readonly HashSet<string> ExactPlaceholderValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "secret",
        "dummy",
        "sample",
        "testing",
        "todo"
    };

    public ValidationReport Validate(string stagingDirectory)
    {
        var report = new ValidationReport();

        if (string.IsNullOrWhiteSpace(stagingDirectory))
        {
            report.Add("STAGING_DIRECTORY", "A staging directory is required.");
            return report;
        }

        string fullDirectory;

        try
        {
            fullDirectory = Path.GetFullPath(stagingDirectory);
        }
        catch
        {
            report.Add("STAGING_DIRECTORY", "The staging directory path is invalid.");
            return report;
        }

        if (!Directory.Exists(fullDirectory))
        {
            report.Add("STAGING_DIRECTORY", "The staging directory does not exist.");
            return report;
        }

        var environmentPath = Path.Combine(fullDirectory, ".env");

        if (!File.Exists(environmentPath))
        {
            report.Add("ENV_FILE", "The staging .env file is missing.");
            return report;
        }

        ValidateEnvironmentFilePermissions(environmentPath, report);

        var parsed = DotEnvParser.Parse(environmentPath);
        report.AddRange(parsed.Failures);

        if (parsed.Failures.Count > 0)
        {
            return report;
        }

        ValidateEnvironmentContract(parsed.Values, report);
        ValidateNetworkAndMail(parsed.Values, report);

        var secretValues = ReadSecretFiles(fullDirectory, report);
        ValidateSymmetricMaterial(parsed.Values, secretValues, report);
        ValidateActivationKeyPair(secretValues, report);

        return report;
    }

    private static void ValidateEnvironmentContract(
        IReadOnlyDictionary<string, string> environment,
        ValidationReport report)
    {
        foreach (var variable in StagingPreflightContract.RequiredVariables)
        {
            if (!environment.TryGetValue(variable, out var value))
            {
                report.Add("ENV_REQUIRED", $"Required variable {variable} is missing.");
                continue;
            }

            if (!StagingPreflightContract.MayBeEmpty(variable) && string.IsNullOrWhiteSpace(value))
            {
                report.Add("ENV_REQUIRED", $"Required variable {variable} is empty.");
                continue;
            }

            if (value.Length > 0 && IsPlaceholder(value))
            {
                report.Add("ENV_PLACEHOLDER", $"Variable {variable} still contains a placeholder value.");
            }
        }

        if (TryGet(environment, "CONTROL_CLOUD_DB_PASSWORD", out var cloudPassword)
            && TryGet(environment, "CONTROL_DESK_DB_PASSWORD", out var deskPassword)
            && CryptographicMaterialValidator.FixedTimeEquals(cloudPassword, deskPassword))
        {
            report.Add("DATABASE_PASSWORD_DISTINCT", "The two database passwords must be different.");
        }

        ValidateDatabasePassword(environment, "CONTROL_CLOUD_DB_PASSWORD", report);
        ValidateDatabasePassword(environment, "CONTROL_DESK_DB_PASSWORD", report);
        ValidatePostgresIdentifier(environment, "CONTROL_CLOUD_DB_NAME", report);
        ValidatePostgresIdentifier(environment, "CONTROL_CLOUD_DB_USER", report);
        ValidatePostgresIdentifier(environment, "CONTROL_DESK_DB_NAME", report);
        ValidatePostgresIdentifier(environment, "CONTROL_DESK_DB_USER", report);

        ValidatePort(environment, "CONTROL_CLOUD_DB_HOST_PORT", "DB_PORT", report);
        ValidatePort(environment, "CONTROL_DESK_DB_HOST_PORT", "DB_PORT", report);

        if (TryParsePort(environment, "CONTROL_CLOUD_DB_HOST_PORT", out var cloudPort)
            && TryParsePort(environment, "CONTROL_DESK_DB_HOST_PORT", out var deskPort)
            && cloudPort == deskPort)
        {
            report.Add("DB_PORT_DISTINCT", "The two database host ports must be different.");
        }
    }

    private static void ValidateNetworkAndMail(
        IReadOnlyDictionary<string, string> environment,
        ValidationReport report)
    {
        var hasCloudHost = TryGet(environment, "CONTROL_CLOUD_HOST", out var cloudHost);
        var hasDeskHost = TryGet(environment, "CONTROL_DESK_HOST", out var deskHost);

        if (hasCloudHost && !IsPublicDnsHost(cloudHost))
        {
            report.Add("PUBLIC_HOST", "CONTROL_CLOUD_HOST must be a non-loopback DNS hostname without a scheme, port, or path.");
        }

        if (hasDeskHost && !IsPublicDnsHost(deskHost))
        {
            report.Add("PUBLIC_HOST", "CONTROL_DESK_HOST must be a non-loopback DNS hostname without a scheme, port, or path.");
        }

        if (hasCloudHost && hasDeskHost && string.Equals(cloudHost, deskHost, StringComparison.OrdinalIgnoreCase))
        {
            report.Add("PUBLIC_HOST_DISTINCT", "The Control Cloud and Control Desk hostnames must be different.");
        }

        if (TryGet(environment, "CLIENT_PORTAL_PUBLIC_URL", out var portalUrl)
            && (!Uri.TryCreate(portalUrl, UriKind.Absolute, out var portalUri)
                || !string.Equals(portalUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrEmpty(portalUri.UserInfo)
                || !string.IsNullOrEmpty(portalUri.Query)
                || !string.IsNullOrEmpty(portalUri.Fragment)
                || portalUri.Port is < 1 or > 65_535
                || !IsPublicDnsHost(portalUri.Host)
                || (hasCloudHost && !string.Equals(portalUri.Host, cloudHost, StringComparison.OrdinalIgnoreCase))))
        {
            report.Add("PORTAL_URL", "CLIENT_PORTAL_PUBLIC_URL must be a non-loopback HTTPS URL on CONTROL_CLOUD_HOST with no credentials, query, or fragment.");
        }

        if (TryGet(environment, "CLIENT_PORTAL_INVITATION_DELIVERY_PROVIDER", out var deliveryProvider)
            && !string.Equals(deliveryProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            report.Add("SMTP_PROVIDER", "The staging invitation delivery provider must be Smtp.");
        }

        if (TryGet(environment, "SMTP_HOST", out var smtpHost) && !IsDnsHost(smtpHost))
        {
            report.Add("SMTP_HOST", "SMTP_HOST must be a non-loopback DNS hostname without a scheme, port, or path.");
        }

        ValidatePort(environment, "SMTP_PORT", "SMTP_PORT", report);

        var hasSmtpUser = environment.TryGetValue("SMTP_USER", out var smtpUser)
                          && !string.IsNullOrEmpty(smtpUser);
        var hasSmtpPassword = environment.TryGetValue("SMTP_PASS", out var smtpPassword)
                              && !string.IsNullOrEmpty(smtpPassword);

        if (hasSmtpUser != hasSmtpPassword)
        {
            report.Add("SMTP_CREDENTIALS", "SMTP_USER and SMTP_PASS must either both be set or both be empty.");
        }

        if (TryGet(environment, "FROM_ADDRESS", out var fromAddress) && !IsMailbox(fromAddress))
        {
            report.Add("FROM_ADDRESS", "FROM_ADDRESS must contain one mailbox address without a display name.");
        }
    }

    private static Dictionary<string, string> ReadSecretFiles(
        string stagingDirectory,
        ValidationReport report)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var secretsDirectory = Path.Combine(stagingDirectory, "secrets");

        foreach (var fileName in StagingPreflightContract.SecretFiles)
        {
            var path = Path.Combine(secretsDirectory, fileName);

            if (!File.Exists(path))
            {
                report.Add("SECRET_FILE", $"Required secret file {fileName} is missing.");
                continue;
            }

            try
            {
                ValidateSecretFilePermissions(path, fileName, report);

                if (new FileInfo(path).Length > MaximumSecretFileBytes)
                {
                    report.Add("SECRET_FILE", $"Secret file {fileName} is not a valid bounded staging secret file.");
                    continue;
                }

                var value = File.ReadAllText(path, new UTF8Encoding(false, true)).Trim();

                if (value.Length == 0 || IsPlaceholder(value))
                {
                    report.Add("SECRET_FILE", $"Secret file {fileName} is empty or contains a placeholder.");
                    continue;
                }

                values.Add(fileName, value);
            }
            catch
            {
                report.Add("SECRET_FILE", $"Secret file {fileName} could not be read safely.");
            }
        }

        return values;
    }

    private static void ValidateSymmetricMaterial(
        IReadOnlyDictionary<string, string> environment,
        IReadOnlyDictionary<string, string> secretFiles,
        ValidationReport report)
    {
        foreach (var variable in StagingPreflightContract.SymmetricEnvironmentVariables)
        {
            if (TryGet(environment, variable, out var value)
                && (!CryptographicMaterialValidator.HasMinimumUtf8Bytes(value, 32)
                    || !CryptographicMaterialValidator.HasMinimumDistinctCharacters(value, 8)
                    || value.Contains('$')
                    || !string.Equals(value, value.Trim(), StringComparison.Ordinal)))
            {
                report.Add("SYMMETRIC_SECRET", $"Variable {variable} must contain at least 32 UTF-8 bytes, at least 8 distinct characters, no dollar sign, and no surrounding whitespace.");
            }
        }

        foreach (var fileName in StagingPreflightContract.SymmetricSecretFiles)
        {
            if (secretFiles.TryGetValue(fileName, out var value)
                && (!CryptographicMaterialValidator.HasMinimumUtf8Bytes(value, 32)
                    || !CryptographicMaterialValidator.HasMinimumDistinctCharacters(value, 8)
                    || value.Contains('\n')
                    || value.Contains('\r')))
            {
                report.Add("SYMMETRIC_SECRET", $"Secret file {fileName} must contain one single-line value of at least 32 UTF-8 bytes and at least 8 distinct characters.");
            }
        }

        ValidateEnvironmentFileMatch(
            environment,
            "CONTROL_DESK_PUBLISHER_SIGNING_SECRET",
            secretFiles,
            "control-desk-publisher-hmac",
            "PUBLISHER_SECRET_MATCH",
            report);
        ValidateEnvironmentFileMatch(
            environment,
            "PROVIDER_ACCESS_SHARED_SECRET",
            secretFiles,
            "provider-access-shared-secret",
            "PROVIDER_SECRET_MATCH",
            report);

        var independentSecrets = new List<string>();
        AddIfPresent(environment, "CONTROL_DESK_SESSION_SIGNING_SECRET", independentSecrets);
        AddIfPresent(environment, "CONTROL_DESK_PUBLISHER_SIGNING_SECRET", independentSecrets);
        AddIfPresent(secretFiles, "control-cloud-entitlement-hmac", independentSecrets);
        AddIfPresent(environment, "PROVIDER_ACCESS_SHARED_SECRET", independentSecrets);
        AddIfPresent(secretFiles, "provider-access-session-hmac", independentSecrets);
        AddIfPresent(secretFiles, "provider-access-totp-hmac", independentSecrets);
        AddIfPresent(environment, "CLIENT_PORTAL_SESSION_SIGNING_SECRET", independentSecrets);
        AddIfPresent(environment, "CLIENT_PORTAL_MFA_PROTECTION_SECRET", independentSecrets);

        for (var left = 0; left < independentSecrets.Count; left++)
        {
            for (var right = left + 1; right < independentSecrets.Count; right++)
            {
                if (CryptographicMaterialValidator.FixedTimeEquals(
                        independentSecrets[left],
                        independentSecrets[right]))
                {
                    report.Add("SECRET_DISTINCT", "Independent signing and protection secrets must not be reused.");
                    return;
                }
            }
        }
    }

    private static void ValidateActivationKeyPair(
        IReadOnlyDictionary<string, string> secretFiles,
        ValidationReport report)
    {
        if (secretFiles.TryGetValue("app-activation-public.pem", out var publicPem)
            && secretFiles.TryGetValue("app-activation-private.pem", out var privatePem)
            && !CryptographicMaterialValidator.IsMatchingP256KeyPair(publicPem, privatePem))
        {
            report.Add("APP_ACTIVATION_KEY_PAIR", "The app-activation PEM files must be a matching ECDSA P-256 private/public key pair.");
        }
    }

    private static void ValidateEnvironmentFileMatch(
        IReadOnlyDictionary<string, string> environment,
        string variable,
        IReadOnlyDictionary<string, string> secretFiles,
        string fileName,
        string code,
        ValidationReport report)
    {
        if (TryGet(environment, variable, out var environmentValue)
            && secretFiles.TryGetValue(fileName, out var fileValue)
            && !CryptographicMaterialValidator.FixedTimeEquals(environmentValue, fileValue))
        {
            report.Add(code, $"Variable {variable} must match secret file {fileName}.");
        }
    }

    private static void ValidatePort(
        IReadOnlyDictionary<string, string> environment,
        string variable,
        string code,
        ValidationReport report)
    {
        if (environment.TryGetValue(variable, out var value)
            && (!int.TryParse(value, out var port) || port is < 1 or > 65_535))
        {
            report.Add(code, $"Variable {variable} must be an integer from 1 through 65535.");
        }
    }

    private static void ValidateDatabasePassword(
        IReadOnlyDictionary<string, string> environment,
        string variable,
        ValidationReport report)
    {
        if (TryGet(environment, variable, out var value)
            && (!CryptographicMaterialValidator.HasMinimumUtf8Bytes(value, 32)
                || value.Any(character => character is ';' or '\'' or '"' or '$' or '#'
                                          || char.IsWhiteSpace(character))))
        {
            report.Add(
                "DATABASE_PASSWORD_FORMAT",
                $"Variable {variable} must contain at least 32 UTF-8 bytes and use a Compose-safe value without unsupported punctuation or whitespace; base64url is recommended.");
        }
    }

    private static void ValidatePostgresIdentifier(
        IReadOnlyDictionary<string, string> environment,
        string variable,
        ValidationReport report)
    {
        if (TryGet(environment, variable, out var value)
            && (value.Length == 0
                || !(value[0] == '_' || char.IsAsciiLetter(value[0]))
                || value.Any(character =>
                    !(character == '_' || char.IsAsciiLetterOrDigit(character)))))
        {
            report.Add(
                "DATABASE_IDENTIFIER",
                $"Variable {variable} must be a simple PostgreSQL identifier containing only ASCII letters, digits, and underscores.");
        }
    }

    private static void ValidateEnvironmentFilePermissions(string path, ValidationReport report)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            const UnixFileMode forbidden = UnixFileMode.GroupRead
                                           | UnixFileMode.GroupWrite
                                           | UnixFileMode.OtherRead
                                           | UnixFileMode.OtherWrite;

            if ((File.GetUnixFileMode(path) & forbidden) != 0)
            {
                report.Add(
                    "ENV_PERMISSIONS",
                    "The staging .env file must not be readable or writable by group or other users.");
            }
        }
        catch
        {
            report.Add("ENV_PERMISSIONS", "The staging .env file permissions could not be verified.");
        }
    }

    private static void ValidateSecretFilePermissions(
        string path,
        string fileName,
        ValidationReport report)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            const UnixFileMode forbidden = UnixFileMode.GroupWrite
                                           | UnixFileMode.OtherRead
                                           | UnixFileMode.OtherWrite;

            if ((File.GetUnixFileMode(path) & forbidden) != 0)
            {
                report.Add(
                    "SECRET_PERMISSIONS",
                    $"Secret file {fileName} must not be group-writable or readable/writable by other users.");
            }
        }
        catch
        {
            report.Add(
                "SECRET_PERMISSIONS",
                $"Permissions for secret file {fileName} could not be verified.");
        }
    }

    private static bool TryParsePort(
        IReadOnlyDictionary<string, string> environment,
        string variable,
        out int port)
    {
        port = 0;
        return environment.TryGetValue(variable, out var value)
               && int.TryParse(value, out port)
               && port is >= 1 and <= 65_535;
    }

    private static bool IsPublicDnsHost(string value) =>
        IsDnsHost(value) && value.Contains('.');

    private static bool IsDnsHost(string value)
    {
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal)
            || Uri.CheckHostName(value) != UriHostNameType.Dns
            || value.Contains('/')
            || value.Contains(':')
            || string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".test", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".example", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool IsMailbox(string value)
    {
        if (!MailAddress.TryCreate(value, out var address))
        {
            return false;
        }

        return address.DisplayName.Length == 0
               && string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlaceholder(string value)
    {
        var trimmed = value.Trim();

        return ExactPlaceholderValues.Contains(trimmed)
               || trimmed.StartsWith("your-", StringComparison.OrdinalIgnoreCase)
               || trimmed.StartsWith("insert-", StringComparison.OrdinalIgnoreCase)
               || PlaceholderMarkers.Any(marker =>
                   trimmed.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryGet(
        IReadOnlyDictionary<string, string> values,
        string key,
        out string value)
    {
        if (values.TryGetValue(key, out var candidate) && !string.IsNullOrWhiteSpace(candidate))
        {
            value = candidate;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static void AddIfPresent(
        IReadOnlyDictionary<string, string> values,
        string key,
        ICollection<string> destination)
    {
        if (TryGet(values, key, out var value))
        {
            destination.Add(value);
        }
    }
}
