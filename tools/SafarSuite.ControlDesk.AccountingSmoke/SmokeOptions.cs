namespace SafarSuite.ControlDesk.AccountingSmoke;

internal sealed record SmokeOptions(
    string Provider,
    string? ConnectionString,
    string? CloudReceiverUrl)
{
    private const string DefaultPostgresConnectionString =
        "Host=localhost;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password";

    public static SmokeOptions Parse(IReadOnlyList<string> args)
    {
        var provider = "inmemory";
        string? connectionString = null;
        string? cloudReceiverUrl = null;

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];

            if (argument.Equals("--help", StringComparison.OrdinalIgnoreCase)
                || argument.Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                throw new SmokeFailureException(
                    "Usage: dotnet run --project tools/SafarSuite.ControlDesk.AccountingSmoke -- [--provider inmemory|postgres] [--connection-string <value>] [--cloud-receiver-url <url>]");
            }

            if (argument.StartsWith("--provider=", StringComparison.OrdinalIgnoreCase))
            {
                provider = argument["--provider=".Length..].Trim();
                continue;
            }

            if (argument.Equals("--provider", StringComparison.OrdinalIgnoreCase))
            {
                provider = ReadValue(args, ref index, "--provider");
                continue;
            }

            if (argument.StartsWith("--connection-string=", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = argument["--connection-string=".Length..].Trim();
                continue;
            }

            if (argument.Equals("--connection-string", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = ReadValue(args, ref index, "--connection-string");
                continue;
            }

            if (argument.StartsWith("--cloud-receiver-url=", StringComparison.OrdinalIgnoreCase))
            {
                cloudReceiverUrl = argument["--cloud-receiver-url=".Length..].Trim();
                continue;
            }

            if (argument.Equals("--cloud-receiver-url", StringComparison.OrdinalIgnoreCase))
            {
                cloudReceiverUrl = ReadValue(args, ref index, "--cloud-receiver-url");
                continue;
            }

            throw new SmokeFailureException($"Unknown argument '{argument}'.");
        }

        provider = provider.Trim().ToLowerInvariant();

        if (provider is not ("inmemory" or "postgres"))
        {
            throw new SmokeFailureException("Smoke provider must be 'inmemory' or 'postgres'.");
        }

        if (provider == "postgres")
        {
            connectionString = FirstNonBlank(
                connectionString,
                Environment.GetEnvironmentVariable("SAFARSUITE_CONTROL_DESK_CONNECTION_STRING"),
                DefaultPostgresConnectionString);
        }

        return new SmokeOptions(provider, connectionString, cloudReceiverUrl);
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string optionName)
    {
        if (index + 1 >= args.Count)
        {
            throw new SmokeFailureException($"{optionName} requires a value.");
        }

        index += 1;

        return args[index].Trim();
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }
}
