using System.Security.Cryptography;
using Xunit;

namespace SafarSuite.StagingPreflight.Tests;

public sealed class StagingPreflightValidatorTests : IDisposable
{
    private readonly string _stagingDirectory = Path.Combine(
        Path.GetTempPath(),
        $"safarsuite-staging-preflight-{Guid.NewGuid():N}");
    private readonly Dictionary<string, string> _environment = new(StringComparer.Ordinal);

    public StagingPreflightValidatorTests()
    {
        Directory.CreateDirectory(Path.Combine(_stagingDirectory, "secrets"));
        SeedValidEnvironment();
        SeedValidSecrets();
        WriteEnvironment();
    }

    [Fact]
    public void Validate_AcceptsCompleteValidFixture()
    {
        var report = Validate();

        Assert.True(report.IsValid, string.Join(Environment.NewLine, report.Failures));
    }

    [Fact]
    public void Validate_RejectsPlaceholder()
    {
        SetEnvironment("CONTROL_CLOUD_DB_PASSWORD", "replace-with-random-password");

        var report = Validate();

        Assert.Contains(report.Failures, failure => failure.Code == "ENV_PLACEHOLDER");
    }

    [Fact]
    public void Validate_RejectsEnvironmentAndFileSecretMismatch()
    {
        SetEnvironment("CONTROL_DESK_PUBLISHER_SIGNING_SECRET", Secret("different-publisher"));

        var report = Validate();

        Assert.Contains(report.Failures, failure => failure.Code == "PUBLISHER_SECRET_MATCH");
    }

    [Fact]
    public void Validate_RejectsDuplicateIndependentSecrets()
    {
        SetEnvironment(
            "CLIENT_PORTAL_MFA_PROTECTION_SECRET",
            _environment["CLIENT_PORTAL_SESSION_SIGNING_SECRET"]);

        var report = Validate();

        Assert.Contains(report.Failures, failure => failure.Code == "SECRET_DISTINCT");
    }

    [Fact]
    public void Validate_RejectsMismatchedEcdsaPair()
    {
        using var unrelatedKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WriteSecret("app-activation-public.pem", unrelatedKey.ExportSubjectPublicKeyInfoPem());

        var report = Validate();

        Assert.Contains(report.Failures, failure => failure.Code == "APP_ACTIVATION_KEY_PAIR");
    }

    [Fact]
    public void Validate_RejectsDuplicateEnvironmentKey()
    {
        File.AppendAllText(
            Path.Combine(_stagingDirectory, ".env"),
            $"{Environment.NewLine}CONTROL_CLOUD_HOST=other.forgeaxis.tech{Environment.NewLine}");

        var report = Validate();

        Assert.Contains(report.Failures, failure => failure.Code == "ENV_DUPLICATE");
    }

    [Fact]
    public void Validate_RejectsDatabasePasswordUnsafeForComposeConnectionString()
    {
        SetEnvironment("CONTROL_CLOUD_DB_PASSWORD", "unsafe;password");

        var report = Validate();

        Assert.Contains(report.Failures, failure => failure.Code == "DATABASE_PASSWORD_FORMAT");
    }

    [Fact]
    public void Validate_RejectsShortDatabasePassword()
    {
        SetEnvironment("CONTROL_CLOUD_DB_PASSWORD", "short-but-compose-safe");

        var report = Validate();

        Assert.Contains(report.Failures, failure => failure.Code == "DATABASE_PASSWORD_FORMAT");
    }

    [Fact]
    public void Validate_RejectsUnsafeDatabaseIdentifier()
    {
        SetEnvironment("CONTROL_CLOUD_DB_NAME", "cloud;Host=elsewhere");

        var report = Validate();

        Assert.Contains(report.Failures, failure => failure.Code == "DATABASE_IDENTIFIER");
    }

    [Fact]
    public void Validate_RejectsSymmetricSecretWithLowCharacterDiversity()
    {
        SetEnvironment("CONTROL_DESK_SESSION_SIGNING_SECRET", new string('a', 40));

        var report = Validate();

        Assert.Contains(report.Failures, failure => failure.Code == "SYMMETRIC_SECRET");
    }

    [Fact]
    public void Validate_RejectsComposeInterpolationInControlledInlineSecret()
    {
        SetEnvironment(
            "CONTROL_DESK_SESSION_SIGNING_SECRET",
            "ControlledSecret_ABCDEFGHIJKLMNOPQRSTUVWXYZ_$NAME");

        var report = Validate();

        Assert.Contains(report.Failures, failure => failure.Code == "SYMMETRIC_SECRET");
    }

    [Fact]
    public void Validate_RejectsEnvironmentLineContinuation()
    {
        File.AppendAllText(
            Path.Combine(_stagingDirectory, ".env"),
            $"{Environment.NewLine}EXTRA_VALUE=continued\\{Environment.NewLine}");

        var report = Validate();

        Assert.Contains(report.Failures, failure => failure.Code == "ENV_FORMAT");
    }

    [Fact]
    public void Validate_RejectsEnvironmentFileWithBroadUnixPermissions()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var environmentPath = Path.Combine(_stagingDirectory, ".env");
        File.SetUnixFileMode(
            environmentPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);

        var report = Validate();

        Assert.Contains(report.Failures, failure => failure.Code == "ENV_PERMISSIONS");
    }

    public void Dispose()
    {
        if (Directory.Exists(_stagingDirectory))
        {
            Directory.Delete(_stagingDirectory, recursive: true);
        }
    }

    private ValidationReport Validate() =>
        new StagingPreflightValidator().Validate(_stagingDirectory);

    private void SeedValidEnvironment()
    {
        _environment["CONTROL_CLOUD_HOST"] = "cloud-staging.forgeaxis.tech";
        _environment["CONTROL_DESK_HOST"] = "desk-staging.forgeaxis.tech";
        _environment["CONTROL_CLOUD_DB_NAME"] = "safarsuite_control_cloud_staging";
        _environment["CONTROL_CLOUD_DB_USER"] = "safarsuite_cloud";
        _environment["CONTROL_CLOUD_DB_PASSWORD"] = "CloudDbPassword_0123456789abcdefABCDEF";
        _environment["CONTROL_CLOUD_DB_HOST_PORT"] = "55432";
        _environment["CONTROL_DESK_DB_NAME"] = "safarsuite_control_desk_staging";
        _environment["CONTROL_DESK_DB_USER"] = "safarsuite_desk";
        _environment["CONTROL_DESK_DB_PASSWORD"] = "DeskDbPassword_0123456789abcdefABCDEF";
        _environment["CONTROL_DESK_DB_HOST_PORT"] = "55433";
        _environment["CONTROL_DESK_SESSION_SIGNING_SECRET"] = Secret("desk-session");
        _environment["CONTROL_DESK_PUBLISHER_SIGNING_KEY_ID"] = "staging-control-desk-202607";
        _environment["CONTROL_DESK_PUBLISHER_SIGNING_SECRET"] = Secret("publisher");
        _environment["CONTROL_CLOUD_ENTITLEMENT_SIGNING_KEY_ID"] = "staging-entitlement-202607";
        _environment["CONTROL_CLOUD_APP_ACTIVATION_SIGNING_KEY_ID"] = "staging-app-activation-202607";
        _environment["PROVIDER_ACCESS_SHARED_SECRET"] = Secret("provider-shared");
        _environment["CLIENT_PORTAL_SESSION_SIGNING_SECRET"] = Secret("client-session");
        _environment["CLIENT_PORTAL_MFA_PROTECTION_SECRET"] = Secret("client-mfa");
        _environment["CLIENT_PORTAL_PUBLIC_URL"] =
            "https://cloud-staging.forgeaxis.tech/client-portal/index.html";
        _environment["CLIENT_PORTAL_INVITATION_DELIVERY_PROVIDER"] = "Smtp";
        _environment["SMTP_HOST"] = "smtp.forgeaxis.tech";
        _environment["SMTP_PORT"] = "587";
        _environment["SMTP_USER"] = string.Empty;
        _environment["SMTP_PASS"] = string.Empty;
        _environment["FROM_ADDRESS"] = "no-reply@forgeaxis.tech";
    }

    private void SeedValidSecrets()
    {
        WriteSecret("control-desk-publisher-hmac", _environment["CONTROL_DESK_PUBLISHER_SIGNING_SECRET"]);
        WriteSecret("control-cloud-entitlement-hmac", Secret("entitlement"));
        WriteSecret("provider-access-shared-secret", _environment["PROVIDER_ACCESS_SHARED_SECRET"]);
        WriteSecret("provider-access-session-hmac", Secret("provider-session"));
        WriteSecret("provider-access-totp-hmac", Secret("provider-totp"));

        using var activationKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        WriteSecret("app-activation-public.pem", activationKey.ExportSubjectPublicKeyInfoPem());
        WriteSecret("app-activation-private.pem", activationKey.ExportPkcs8PrivateKeyPem());
    }

    private void SetEnvironment(string name, string value)
    {
        _environment[name] = value;
        WriteEnvironment();
    }

    private void WriteEnvironment()
    {
        var path = Path.Combine(_stagingDirectory, ".env");
        File.WriteAllLines(
            path,
            _environment.Select(pair => $"{pair.Key}={pair.Value}"));

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    private void WriteSecret(string fileName, string value)
    {
        var path = Path.Combine(_stagingDirectory, "secrets", fileName);
        File.WriteAllText(path, value);

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
        }
    }

    private static string Secret(string purpose) =>
        $"{purpose}_ABCDEFGHIJKLMNOPQRSTUVWXYZ_0123456789";
}
