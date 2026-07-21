using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SafarSuite.ControlDesk.Api.Tests;

public class ControlDeskApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@example.test";
    public const string ReportReaderEmail = "reports@example.test";
    public const string DiagnosticsReaderEmail = "diagnostics@example.test";
    public const string Password = "Test-password-123!";

    public MutableTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 7, 17, 0, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "InMemory",
                ["Persistence:AllowInMemoryForTests"] = "true",
                ["ControlDesk:OperatorAccess:SessionMinutes"] = "5",
                ["ControlDesk:OperatorAccess:SessionSigningSecret"] = "integration-test-session-signing-secret-at-least-32-bytes",
                ["ControlDesk:OperatorAccess:Users:0:UserId"] = "test-admin",
                ["ControlDesk:OperatorAccess:Users:0:Email"] = AdminEmail,
                ["ControlDesk:OperatorAccess:Users:0:FullName"] = "Test Administrator",
                ["ControlDesk:OperatorAccess:Users:0:PasswordHash"] = HashPassword(Password),
                ["ControlDesk:OperatorAccess:Users:0:Status"] = "Active",
                ["ControlDesk:OperatorAccess:Users:0:Roles:0"] = "Administrator",
                ["ControlDesk:OperatorAccess:Users:0:Scopes:0"] = "control-desk:admin",
                ["ControlDesk:OperatorAccess:Users:1:UserId"] = "test-report-reader",
                ["ControlDesk:OperatorAccess:Users:1:Email"] = ReportReaderEmail,
                ["ControlDesk:OperatorAccess:Users:1:FullName"] = "Test Report Reader",
                ["ControlDesk:OperatorAccess:Users:1:PasswordHash"] = HashPassword(Password),
                ["ControlDesk:OperatorAccess:Users:1:Status"] = "Active",
                ["ControlDesk:OperatorAccess:Users:1:Roles:0"] = "Auditor",
                ["ControlDesk:OperatorAccess:Users:1:Scopes:0"] = "reports:read",
                ["ControlDesk:OperatorAccess:Users:2:UserId"] = "test-diagnostics-reader",
                ["ControlDesk:OperatorAccess:Users:2:Email"] = DiagnosticsReaderEmail,
                ["ControlDesk:OperatorAccess:Users:2:FullName"] = "Test Diagnostics Reader",
                ["ControlDesk:OperatorAccess:Users:2:PasswordHash"] = HashPassword(Password),
                ["ControlDesk:OperatorAccess:Users:2:Status"] = "Active",
                ["ControlDesk:OperatorAccess:Users:2:Roles:0"] = "SupportOperator",
                ["ControlDesk:OperatorAccess:Users:2:Scopes:0"] = "diagnostics:read"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    private static string HashPassword(string password)
    {
        const int iterations = 10_000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            32);

        return $"pbkdf2-sha256.{iterations}.{Base64UrlEncode(salt)}.{Base64UrlEncode(hash)}";
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
