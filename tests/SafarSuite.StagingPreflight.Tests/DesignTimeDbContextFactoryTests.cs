using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;
using SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;
using Xunit;

namespace SafarSuite.StagingPreflight.Tests;

public sealed class DesignTimeDbContextFactoryTests
{
    private const string CloudConnectionVariable = "SAFARSUITE_CONTROL_CLOUD_CONNECTION_STRING";
    private const string DeskConnectionVariable = "SAFARSUITE_CONTROL_DESK_CONNECTION_STRING";
    private const string FallbackVariable = "SAFARSUITE_ALLOW_DEVELOPMENT_DB_FALLBACK";

    [Theory]
    [InlineData(FactoryKind.ControlCloud)]
    [InlineData(FactoryKind.ControlDesk)]
    public void CreateDbContext_RejectsMissingExplicitConnectionWithoutFallback(FactoryKind factoryKind)
    {
        using var environment = SetFactoryEnvironment();

        var exception = Assert.Throws<InvalidOperationException>(() => CreateDbContext(factoryKind));

        Assert.Contains(ConnectionVariable(factoryKind), exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(FactoryKind.ControlCloud)]
    [InlineData(FactoryKind.ControlDesk)]
    public void CreateDbContext_AcceptsExplicitConnection(FactoryKind factoryKind)
    {
        const string explicitConnection =
            "Host=explicit-staging-db;Port=55432;Database=safarsuite_test;Username=safarsuite_test;Password=ExplicitDatabasePassword_0123456789";
        using var environment = SetFactoryEnvironment(factoryKind, explicitConnection);
        using var context = CreateDbContext(factoryKind);

        Assert.Equal(explicitConnection, context.Database.GetConnectionString());
    }

    [Theory]
    [InlineData(FactoryKind.ControlCloud)]
    [InlineData(FactoryKind.ControlDesk)]
    public void CreateDbContext_AcceptsExplicitDevelopmentFallbackOptIn(FactoryKind factoryKind)
    {
        using var environment = SetFactoryEnvironment(allowFallback: true);
        using var context = CreateDbContext(factoryKind);
        var connectionString = context.Database.GetConnectionString();

        Assert.NotNull(connectionString);
        Assert.Contains("Host=localhost", connectionString, StringComparison.Ordinal);
        Assert.Contains("Port=54329", connectionString, StringComparison.Ordinal);
    }

    public enum FactoryKind
    {
        ControlCloud,
        ControlDesk
    }

    private static ProcessEnvironmentScope SetFactoryEnvironment(
        FactoryKind? explicitFactory = null,
        string? explicitConnection = null,
        bool allowFallback = false) =>
        new(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [CloudConnectionVariable] = explicitFactory == FactoryKind.ControlCloud
                ? explicitConnection
                : null,
            [DeskConnectionVariable] = explicitFactory == FactoryKind.ControlDesk
                ? explicitConnection
                : null,
            [FallbackVariable] = allowFallback ? "true" : null
        });

    private static DbContext CreateDbContext(FactoryKind factoryKind) =>
        factoryKind switch
        {
            FactoryKind.ControlCloud => new ControlCloudDbContextFactory().CreateDbContext([]),
            FactoryKind.ControlDesk => new ControlDeskDbContextFactory().CreateDbContext([]),
            _ => throw new ArgumentOutOfRangeException(nameof(factoryKind), factoryKind, null)
        };

    private static string ConnectionVariable(FactoryKind factoryKind) =>
        factoryKind switch
        {
            FactoryKind.ControlCloud => CloudConnectionVariable,
            FactoryKind.ControlDesk => DeskConnectionVariable,
            _ => throw new ArgumentOutOfRangeException(nameof(factoryKind), factoryKind, null)
        };
}
