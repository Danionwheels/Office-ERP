using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace SafarSuite.ControlDesk.Api.Composition;

public static class ControlDeskFileLoggingExtensions
{
    private const long FileSizeLimitBytes = 20L * 1024L * 1024L;
    private const int MinimumRetainedFileCount = 2;
    private const int MaximumRetainedFileCount = 100;
    private const int MinimumRetainedDays = 1;
    private const int MaximumRetainedDays = 90;

    public static WebApplicationBuilder AddControlDeskRetainedFileLogging(
        this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new ControlDeskFileLoggingOptions();
        builder.Configuration
            .GetSection(ControlDeskFileLoggingOptions.SectionName)
            .Bind(options);

        if (!options.Enabled)
        {
            return builder;
        }

        ValidateRetention(options);

        var logDirectory = ControlDeskLogPathResolver.ResolveDirectory(options.DirectoryPath);
        PrepareDirectory(logDirectory);
        var logFilePath = Path.Combine(logDirectory, "control-desk-.jsonl");

        var fileLogger = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "SafarSuite Control Desk")
            .WriteTo.File(
                new JsonFormatter(renderMessage: true),
                logFilePath,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: FileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                retainedFileTimeLimit: TimeSpan.FromDays(options.RetainedDays),
                buffered: false,
                shared: false)
            .CreateLogger();

        builder.Logging.AddSerilog(fileLogger, dispose: true);

        return builder;
    }

    private static void ValidateRetention(ControlDeskFileLoggingOptions options)
    {
        if (options.RetainedFileCountLimit is < MinimumRetainedFileCount or > MaximumRetainedFileCount)
        {
            throw new InvalidOperationException(
                $"Control Desk retained file count must be between {MinimumRetainedFileCount} and {MaximumRetainedFileCount}.");
        }

        if (options.RetainedDays is < MinimumRetainedDays or > MaximumRetainedDays)
        {
            throw new InvalidOperationException(
                $"Control Desk retained log days must be between {MinimumRetainedDays} and {MaximumRetainedDays}.");
        }
    }

    private static void PrepareDirectory(string directoryPath)
    {
        try
        {
            Directory.CreateDirectory(directoryPath);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException
                                          or IOException
                                          or ArgumentException
                                          or NotSupportedException)
        {
            throw new InvalidOperationException(
                "Control Desk retained file logging directory could not be prepared.");
        }
    }
}
