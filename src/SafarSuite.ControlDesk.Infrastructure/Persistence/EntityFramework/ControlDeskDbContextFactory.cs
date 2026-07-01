using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

public sealed class ControlDeskDbContextFactory : IDesignTimeDbContextFactory<ControlDeskDbContext>
{
    private const string DefaultDevelopmentConnectionString =
        "Host=localhost;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password";

    public ControlDeskDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SAFARSUITE_CONTROL_DESK_CONNECTION_STRING")
            ?? DefaultDevelopmentConnectionString;

        var options = new DbContextOptionsBuilder<ControlDeskDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "control"))
            .Options;

        return new ControlDeskDbContext(options);
    }
}
