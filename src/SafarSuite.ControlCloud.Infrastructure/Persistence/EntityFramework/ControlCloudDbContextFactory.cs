using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class ControlCloudDbContextFactory : IDesignTimeDbContextFactory<ControlCloudDbContext>
{
    private const string DefaultDevelopmentConnectionString =
        "Host=localhost;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password";

    public ControlCloudDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();

        var options = new DbContextOptionsBuilder<ControlCloudDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "cloud"))
            .Options;

        return new ControlCloudDbContext(options);
    }

    private static string ResolveConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable(
            "SAFARSUITE_CONTROL_CLOUD_CONNECTION_STRING");

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        var allowDevelopmentFallback = Environment.GetEnvironmentVariable(
            "SAFARSUITE_ALLOW_DEVELOPMENT_DB_FALLBACK");

        if (string.Equals(allowDevelopmentFallback, "true", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultDevelopmentConnectionString;
        }

        throw new InvalidOperationException(
            "SAFARSUITE_CONTROL_CLOUD_CONNECTION_STRING is required for EF operations. Set SAFARSUITE_ALLOW_DEVELOPMENT_DB_FALLBACK=true only for deliberate local development tooling.");
    }
}
