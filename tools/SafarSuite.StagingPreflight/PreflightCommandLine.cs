namespace SafarSuite.StagingPreflight;

internal sealed record PreflightCommandLine(
    string? StagingDirectory,
    bool ShowHelp,
    string? FailureMessage)
{
    public static PreflightCommandLine Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new PreflightCommandLine(Path.Combine("deploy", "staging"), false, null);
        }

        if (args.Length == 1 && (args[0] is "-h" or "--help"))
        {
            return new PreflightCommandLine(null, true, null);
        }

        if (args.Length == 2
            && args[0] is "--staging-directory" or "--staging-dir"
            && !string.IsNullOrWhiteSpace(args[1]))
        {
            return new PreflightCommandLine(args[1], false, null);
        }

        return new PreflightCommandLine(
            null,
            false,
            "Use --staging-directory <path>, or --help for usage.");
    }
}
