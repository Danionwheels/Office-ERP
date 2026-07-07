namespace SafarSuite.LocalServer.Api.Modules.LocalServer;

public sealed class LocalServerRuntimeAccessOptions
{
    public const string SectionName = "LocalServer:RuntimeAccess";
    public const string AccessKeyHeaderName = "X-SafarSuite-Local-Api-Key";
    public const string AccessKeyEnvironmentVariable = "SAFARSUITE_LOCAL_API_ACCESS_KEY";

    public string SharedSecret { get; set; } = string.Empty;

    public static LocalServerRuntimeAccessOptions FromConfiguration(IConfiguration configuration)
    {
        var options = configuration.GetSection(SectionName).Get<LocalServerRuntimeAccessOptions>()
            ?? new LocalServerRuntimeAccessOptions();
        var environmentSecret = configuration[AccessKeyEnvironmentVariable]?.Trim();

        if (!string.IsNullOrWhiteSpace(environmentSecret))
        {
            options.SharedSecret = environmentSecret;
        }

        return options;
    }
}
