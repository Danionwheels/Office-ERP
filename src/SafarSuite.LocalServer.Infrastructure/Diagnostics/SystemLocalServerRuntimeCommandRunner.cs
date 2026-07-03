using System.ComponentModel;
using System.Diagnostics;

namespace SafarSuite.LocalServer.Infrastructure.Diagnostics;

public sealed class SystemLocalServerRuntimeCommandRunner
    : ILocalServerRuntimeCommandRunner
{
    public async Task<LocalServerRuntimeCommandResult> RunAsync(
        string fileName,
        IReadOnlyCollection<string> arguments,
        string? workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory)
                && Directory.Exists(workingDirectory))
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            foreach (var argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (!process.Start())
            {
                return new LocalServerRuntimeCommandResult(
                    ExitCode: -1,
                    StandardOutput: "",
                    StandardError: $"Command '{fileName}' could not be started.",
                    TimedOut: false);
            }

            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var waitTask = process.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(timeout, cancellationToken);
            var completedTask = await Task.WhenAny(waitTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                TryKill(process);

                return new LocalServerRuntimeCommandResult(
                    ExitCode: -1,
                    StandardOutput: await ReadCompletedAsync(standardOutputTask),
                    StandardError: await ReadCompletedAsync(standardErrorTask),
                    TimedOut: true);
            }

            await waitTask;

            return new LocalServerRuntimeCommandResult(
                process.ExitCode,
                await standardOutputTask,
                await standardErrorTask,
                TimedOut: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Win32Exception exception)
        {
            return new LocalServerRuntimeCommandResult(
                ExitCode: -1,
                StandardOutput: "",
                StandardError: exception.Message,
                TimedOut: false);
        }
        catch (InvalidOperationException exception)
        {
            return new LocalServerRuntimeCommandResult(
                ExitCode: -1,
                StandardOutput: "",
                StandardError: exception.Message,
                TimedOut: false);
        }
    }

    private static async Task<string> ReadCompletedAsync(Task<string> task)
    {
        return task.IsCompletedSuccessfully
            ? await task
            : "";
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }
    }
}
