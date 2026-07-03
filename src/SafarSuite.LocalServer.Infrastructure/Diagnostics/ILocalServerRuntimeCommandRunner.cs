namespace SafarSuite.LocalServer.Infrastructure.Diagnostics;

public interface ILocalServerRuntimeCommandRunner
{
    Task<LocalServerRuntimeCommandResult> RunAsync(
        string fileName,
        IReadOnlyCollection<string> arguments,
        string? workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public sealed record LocalServerRuntimeCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut)
{
    public bool IsSuccess => ExitCode == 0 && !TimedOut;
}
