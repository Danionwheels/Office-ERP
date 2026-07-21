namespace SafarSuite.ConnectedAcceptance;

internal sealed record ConnectedAcceptanceOptions(
    Uri ControlDeskBaseUrl,
    Uri ControlCloudBaseUrl,
    Uri LocalServerBaseUrl,
    string OperatorEmail,
    string OperatorPassword,
    string CloudSigningKeyId,
    string CloudSigningSecret,
    string CloudSourceEnvironment,
    string EvidencePath,
    string? LocalImportAuditPath)
{
    public const string Usage =
        "Usage: dotnet run --project tools/SafarSuite.ConnectedAcceptance -- " +
        "[--control-desk-url <url>] [--control-cloud-url <url>] [--local-server-url <url>] " +
        "[--cloud-source-environment <name>] [--evidence-path <path>] " +
        "[--local-import-audit-path <path>]. " +
        "Credentials must be supplied through SAFARSUITE_ACCEPTANCE_OPERATOR_EMAIL, " +
        "SAFARSUITE_ACCEPTANCE_OPERATOR_PASSWORD, SAFARSUITE_ACCEPTANCE_CLOUD_SIGNING_KEY_ID, " +
        "and SAFARSUITE_ACCEPTANCE_CLOUD_SIGNING_SECRET.";

    public static ConnectedAcceptanceOptions Parse(IReadOnlyList<string> args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Count; index += 1)
        {
            var argument = args[index];

            if (argument is "--help" or "-h")
            {
                throw new ConnectedAcceptanceFailureException(Usage);
            }

            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ConnectedAcceptanceFailureException($"Unknown argument '{argument}'.");
            }

            var separatorIndex = argument.IndexOf('=');

            if (separatorIndex > 2)
            {
                values[argument[2..separatorIndex]] = argument[(separatorIndex + 1)..].Trim();
                continue;
            }

            if (index + 1 >= args.Count)
            {
                throw new ConnectedAcceptanceFailureException($"{argument} requires a value.");
            }

            values[argument[2..]] = args[++index].Trim();
        }

        var defaultEvidencePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "artifacts",
            "connected-acceptance",
            $"connected-acceptance-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json");

        return new ConnectedAcceptanceOptions(
            ReadUri(values, "control-desk-url", "http://127.0.0.1:5188"),
            ReadUri(values, "control-cloud-url", "http://127.0.0.1:5127"),
            ReadUri(values, "local-server-url", "http://127.0.0.1:51046"),
            ReadRequiredEnvironment("SAFARSUITE_ACCEPTANCE_OPERATOR_EMAIL"),
            ReadRequiredEnvironment("SAFARSUITE_ACCEPTANCE_OPERATOR_PASSWORD"),
            ReadRequiredEnvironment("SAFARSUITE_ACCEPTANCE_CLOUD_SIGNING_KEY_ID"),
            ReadRequiredEnvironment("SAFARSUITE_ACCEPTANCE_CLOUD_SIGNING_SECRET"),
            ReadValue(values, "cloud-source-environment", "Development"),
            ReadValue(values, "evidence-path", defaultEvidencePath),
            ReadOptionalValue(values, "local-import-audit-path"));
    }

    private static Uri ReadUri(
        IReadOnlyDictionary<string, string> values,
        string key,
        string fallback)
    {
        var value = ReadValue(values, key, fallback);

        return Uri.TryCreate(value.TrimEnd('/') + "/", UriKind.Absolute, out var uri)
            ? uri
            : throw new ConnectedAcceptanceFailureException($"--{key} must be an absolute URL.");
    }

    private static string ReadRequiredEnvironment(string name)
    {
        var value = Environment.GetEnvironmentVariable(name)?.Trim();

        return string.IsNullOrWhiteSpace(value)
            ? throw new ConnectedAcceptanceFailureException($"Environment variable {name} is required.")
            : value;
    }

    private static string ReadValue(
        IReadOnlyDictionary<string, string> values,
        string key,
        string fallback)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static string? ReadOptionalValue(
        IReadOnlyDictionary<string, string> values,
        string key)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }
}
