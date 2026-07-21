namespace SafarSuite.LocalServer.Api.Modules.LocalServer;

public sealed class LocalServerManagerSessionOptions
{
    public const string SectionName = "LocalServer:ManagerSessions";

    private const string SigningKeyIdEnvironmentVariable = "SAFARSUITE_LOCAL_MANAGER_SESSION_SIGNING_KEY_ID";
    private const string SigningSecretEnvironmentVariable = "SAFARSUITE_LOCAL_MANAGER_SESSION_SIGNING_SECRET";
    private const string SessionMinutesEnvironmentVariable = "SAFARSUITE_LOCAL_MANAGER_SESSION_MINUTES";
    private const string UserSessionsSigningKeyIdConfigurationKey = "UserSessions:SigningKeyId";
    private const string UserSessionsSigningSecretConfigurationKey = "UserSessions:SigningSecret";

    public string SigningKeyId { get; set; } = "local-manager-session-dev";

    public string SigningSecret { get; set; } = string.Empty;

    public int SessionMinutes { get; set; } = 60;

    public static LocalServerManagerSessionOptions FromConfiguration(IConfiguration configuration)
    {
        var options = configuration.GetSection(SectionName).Get<LocalServerManagerSessionOptions>()
            ?? new LocalServerManagerSessionOptions();

        ApplyEnvironmentOverride(configuration, SigningKeyIdEnvironmentVariable, value => options.SigningKeyId = value);
        ApplyEnvironmentOverride(configuration, SigningSecretEnvironmentVariable, value => options.SigningSecret = value);
        ApplyConfigurationFallback(
            configuration,
            UserSessionsSigningKeyIdConfigurationKey,
            options.SigningKeyId == "local-manager-session-dev" ? string.Empty : options.SigningKeyId,
            value => options.SigningKeyId = value);
        ApplyConfigurationFallback(configuration, UserSessionsSigningSecretConfigurationKey, options.SigningSecret, value => options.SigningSecret = value);
        ApplyEnvironmentOverride(configuration, SessionMinutesEnvironmentVariable, value =>
        {
            if (int.TryParse(value, out var parsed))
            {
                options.SessionMinutes = parsed;
            }
        });

        options.SigningKeyId = NormalizeOptional(options.SigningKeyId);
        options.SigningSecret = NormalizeOptional(options.SigningSecret);
        options.SessionMinutes = Math.Clamp(options.SessionMinutes, 5, 1440);

        return options;
    }

    private static void ApplyEnvironmentOverride(
        IConfiguration configuration,
        string key,
        Action<string> apply)
    {
        var value = configuration[key]?.Trim();

        if (!string.IsNullOrWhiteSpace(value))
        {
            apply(value);
        }
    }

    private static void ApplyConfigurationFallback(
        IConfiguration configuration,
        string key,
        string currentValue,
        Action<string> apply)
    {
        if (!string.IsNullOrWhiteSpace(currentValue))
        {
            return;
        }

        var value = configuration[key]?.Trim();

        if (!string.IsNullOrWhiteSpace(value))
        {
            apply(value);
        }
    }

    private static string NormalizeOptional(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
