using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class FileControlCloudInstallationDiagnosticReportRepository
    : IControlCloudInstallationDiagnosticReportRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileControlCloudInstallationDiagnosticReportRepository(
        ControlCloudDiagnosticsOptions options)
    {
        _storePath = ResolveStorePath(options.DiagnosticStorePath);
    }

    public async Task AddAsync(
        ControlCloudInstallationDiagnosticReport report,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var reports = await ReadAllAsync(cancellationToken);
            reports.Add(report);
            await WriteAllAsync(reports, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ControlCloudInstallationDiagnosticReport?> GetLatestByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var cleanInstallationId = installationId.Trim();
            var reports = await ReadAllAsync(cancellationToken);

            return reports
                .Where(report => string.Equals(report.InstallationId, cleanInstallationId, StringComparison.Ordinal))
                .OrderBy(report => report.ReceivedAtUtc)
                .ThenBy(report => report.DiagnosticReportId)
                .LastOrDefault();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<ControlCloudInstallationDiagnosticReport>> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return [];
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        var records = await JsonSerializer.DeserializeAsync<List<ControlCloudInstallationDiagnosticReport>>(
            stream,
            JsonOptions,
            cancellationToken);

        return records ?? [];
    }

    private async Task WriteAllAsync(
        IReadOnlyCollection<ControlCloudInstallationDiagnosticReport> reports,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_storePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var orderedReports = reports
            .OrderBy(report => report.ReceivedAtUtc)
            .ThenBy(report => report.DiagnosticReportId)
            .ToArray();

        await using var stream = new FileStream(
            _storePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, orderedReports, JsonOptions, cancellationToken);
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/control-cloud-installation-diagnostics.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
