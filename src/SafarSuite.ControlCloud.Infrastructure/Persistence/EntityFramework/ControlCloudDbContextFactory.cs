using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

public sealed class ControlCloudDbContextFactory : IDesignTimeDbContextFactory<ControlCloudDbContext>
{
    private const string DefaultDevelopmentConnectionString =
        "Host=localhost;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password";

    public ControlCloudDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SAFARSUITE_CONTROL_CLOUD_CONNECTION_STRING")
            ?? DefaultDevelopmentConnectionString;

        var options = new DbContextOptionsBuilder<ControlCloudDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "cloud"))
            .Options;

        return new ControlCloudDbContext(options);
    }
}
