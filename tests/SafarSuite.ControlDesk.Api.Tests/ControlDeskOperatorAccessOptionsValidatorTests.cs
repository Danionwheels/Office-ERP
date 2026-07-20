using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SafarSuite.ControlDesk.Api.Modules.Auth;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class ControlDeskOperatorAccessOptionsValidatorTests
{
    [Fact]
    public void Postgres_production_accepts_no_configured_users_with_external_signing_secret()
    {
        var validator = CreateValidator("Production", "Postgres");
        var options = ValidOptions();

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Postgres_production_rejects_configuration_users()
    {
        var validator = CreateValidator("Production", "Postgres");
        var options = ValidOptions();
        options.Users.Add(ValidUser());

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure =>
            failure.Contains("must not be supplied through configuration", StringComparison.Ordinal));
    }

    [Fact]
    public void Development_in_memory_requires_an_active_fixture_operator()
    {
        var validator = CreateValidator("Development", "InMemory");

        var result = validator.Validate(null, ValidOptions());

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure =>
            failure.Contains("requires at least one active fixture", StringComparison.Ordinal));
    }

    [Fact]
    public void Development_in_memory_accepts_explicit_fixture_operator()
    {
        var validator = CreateValidator("Development", "InMemory");
        var options = ValidOptions();
        options.Users.Add(ValidUser());

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Production_fails_closed_without_a_signing_secret()
    {
        var validator = CreateValidator("Production", "Postgres");
        var options = ValidOptions();
        options.SessionSigningSecret = string.Empty;

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure =>
            failure.Contains("at least 32 characters", StringComparison.Ordinal));
    }

    private static ControlDeskOperatorAccessOptionsValidator CreateValidator(
        string environmentName,
        string persistenceProvider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = persistenceProvider
            })
            .Build();

        return new ControlDeskOperatorAccessOptionsValidator(
            new TestHostEnvironment(environmentName),
            configuration);
    }

    private static ControlDeskOperatorAccessOptions ValidOptions() => new()
    {
        SessionMinutes = 480,
        SessionSigningSecret = "production-test-signing-secret-at-least-32-characters"
    };

    private static ControlDeskOperatorUserOptions ValidUser() => new()
    {
        UserId = "fixture-admin",
        Email = "fixture.admin@example.test",
        FullName = "Fixture Administrator",
        PasswordHash = "pbkdf2-sha256.120000.AAECAwQFBgcICQoLDA0ODw.GRxYGMDYtjN-kbO2SA72GEAvAeXewYCXAAkTWoXMTxk",
        Status = "Active",
        Roles = ["Administrator"],
        Scopes = ["control-desk:admin"]
    };

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "SafarSuite.ControlDesk.Api.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
