using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SafarSuite.ControlDesk.Api.Composition;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class ControlDeskHostConfigurationValidatorTests
{
    [Fact]
    public void Validate_RejectsInMemoryPersistenceInProduction()
    {
        var configuration = BuildConfiguration(("Persistence:Provider", "InMemory"));
        var environment = new TestHostEnvironment("Production");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ControlDeskHostConfigurationValidator.Validate(configuration, environment));

        Assert.Contains("requires PostgreSQL", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsTestingInMemoryWithoutExplicitOverride()
    {
        var configuration = BuildConfiguration(("Persistence:Provider", "InMemory"));
        var environment = new TestHostEnvironment("Testing");

        Assert.Throws<InvalidOperationException>(() =>
            ControlDeskHostConfigurationValidator.Validate(configuration, environment));
    }

    [Fact]
    public void Validate_AllowsExplicitTestingInMemoryOverride()
    {
        var configuration = BuildConfiguration(
            ("Persistence:Provider", "InMemory"),
            ("Persistence:AllowInMemoryForTests", "true"));
        var environment = new TestHostEnvironment("Testing");

        ControlDeskHostConfigurationValidator.Validate(configuration, environment);
    }

    [Fact]
    public void Validate_AllowsPostgresInProduction()
    {
        var configuration = BuildConfiguration(
            ("Persistence:Provider", "Postgres"),
            ("ConnectionStrings:ControlDesk", "Host=127.0.0.1;Database=safarsuite_office;Username=safarsuite_office;Password=Office-test-secret-42"));
        var environment = new TestHostEnvironment("Production");

        ControlDeskHostConfigurationValidator.Validate(configuration, environment);
    }

    [Fact]
    public void Validate_RejectsMissingPostgresConnection()
    {
        var configuration = BuildConfiguration(("Persistence:Provider", "Postgres"));
        var environment = new TestHostEnvironment("Production");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ControlDeskHostConfigurationValidator.Validate(configuration, environment));

        Assert.Contains("ConnectionStrings:ControlDesk is required", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsDevelopmentPostgresCredentialsOutsideDevelopment()
    {
        var configuration = BuildConfiguration(
            ("Persistence:Provider", "Postgres"),
            ("ConnectionStrings:ControlDesk", "Host=localhost;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password"));
        var environment = new TestHostEnvironment("Production");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ControlDeskHostConfigurationValidator.Validate(configuration, environment));

        Assert.Contains("must not use development", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_AllowsDevelopmentPostgresCredentialsOnlyInDevelopment()
    {
        var configuration = BuildConfiguration(
            ("Persistence:Provider", "Postgres"),
            ("ConnectionStrings:ControlDesk", "Host=localhost;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password"));
        var environment = new TestHostEnvironment("Development");

        ControlDeskHostConfigurationValidator.Validate(configuration, environment);
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(value => value.Key, value => (string?)value.Value))
            .Build();

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "SafarSuite.ControlDesk.Api.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
