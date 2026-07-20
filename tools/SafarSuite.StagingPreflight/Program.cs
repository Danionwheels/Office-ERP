namespace SafarSuite.StagingPreflight;

public static class Program
{
    public static int Main(string[] args)
    {
        var commandLine = PreflightCommandLine.Parse(args);

        if (commandLine.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        if (commandLine.FailureMessage is not null)
        {
            Console.Error.WriteLine($"[FAIL] ARGUMENTS: {commandLine.FailureMessage}");
            return 2;
        }

        try
        {
            var report = new StagingPreflightValidator().Validate(commandLine.StagingDirectory!);

            if (report.IsValid)
            {
                Console.WriteLine("[PASS] Staging configuration and secret material passed preflight validation.");
                return 0;
            }

            foreach (var failure in report.Failures)
            {
                Console.Error.WriteLine($"[FAIL] {failure.Code}: {failure.Message}");
            }

            return 1;
        }
        catch
        {
            Console.Error.WriteLine("[FAIL] PREFLIGHT: Validation could not be completed safely.");
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("SafarSuite staging preflight");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project tools/SafarSuite.StagingPreflight -- [--staging-directory <path>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --staging-directory <path>  Staging bundle directory (default: deploy/staging).");
        Console.WriteLine("  --staging-dir <path>        Alias for --staging-directory.");
        Console.WriteLine("  -h, --help            Show this help text.");
        Console.WriteLine();
        Console.WriteLine("The tool never prints secret values; validation does not generate secrets.");
    }
}
