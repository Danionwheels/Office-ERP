namespace SafarSuite.ControlDesk.Api.Composition;

public static class ControlDeskHostConfigurationValidator
{
    private const string InMemoryProvider = "InMemory";
    private const string PostgresProvider = "Postgres";
    private const string TestingEnvironment = "Testing";
    private const string TestOverrideKey = "Persistence:AllowInMemoryForTests";

    private static readonly string[] DevelopmentConnectionMarkers =
    [
        "safarsuite_dev_password",
        "change-before",
        "replace-with",
        "placeholder"
    ];

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var provider = configuration.GetValue<string>("Persistence:Provider") ?? InMemoryProvider;

        if (provider.Equals(PostgresProvider, StringComparison.OrdinalIgnoreCase))
        {
            ValidatePostgresConnection(configuration, environment);
            return;
        }

        if (!provider.Equals(InMemoryProvider, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (environment.IsDevelopment()
            || (environment.IsEnvironment(TestingEnvironment)
                && configuration.GetValue<bool>(TestOverrideKey)))
        {
            return;
        }

        throw new InvalidOperationException(
            "Persistence:Provider=InMemory is permitted only in Development or in the explicit Testing harness. " +
            "SafarSuite Control Desk requires PostgreSQL for every deployable environment.");
    }

    private static void ValidatePostgresConnection(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("ControlDesk")?.Trim() ?? string.Empty;

        if (connectionString.Length == 0)
        {
            throw new InvalidOperationException(
                "ConnectionStrings:ControlDesk is required when Persistence:Provider is Postgres.");
        }

        if (environment.IsDevelopment())
        {
            return;
        }

        if (DevelopmentConnectionMarkers.Any(marker =>
                connectionString.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:ControlDesk must not use development or placeholder credentials outside Development.");
        }
    }
}
