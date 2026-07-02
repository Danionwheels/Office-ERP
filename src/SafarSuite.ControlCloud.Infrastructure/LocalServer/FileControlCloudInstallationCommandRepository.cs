using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class FileControlCloudInstallationCommandRepository
    : IControlCloudInstallationCommandRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileControlCloudInstallationCommandRepository(ControlCloudCommandQueueOptions options)
    {
        _storePath = ResolveStorePath(options.CommandStorePath);
    }

    public async Task<ControlCloudInstallationCommand?> GetByCommandIdAsync(
        Guid commandId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            return (await ReadAllAsync(cancellationToken))
                .SingleOrDefault(command => command.CommandId == commandId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ControlCloudInstallationCommand?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var cleanIdempotencyKey = idempotencyKey.Trim();

        await _gate.WaitAsync(cancellationToken);

        try
        {
            return (await ReadAllAsync(cancellationToken))
                .SingleOrDefault(command => command.IdempotencyKey == cleanIdempotencyKey);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<long> GetLatestCommandVersionAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();

        await _gate.WaitAsync(cancellationToken);

        try
        {
            return (await ReadAllAsync(cancellationToken))
                .Where(command => command.InstallationId == cleanInstallationId)
                .Select(command => command.CommandVersion)
                .DefaultIfEmpty(0)
                .Max();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyCollection<ControlCloudInstallationCommand>> ListPendingAsync(
        string installationId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();

        await _gate.WaitAsync(cancellationToken);

        try
        {
            return (await ReadAllAsync(cancellationToken))
                .Where(command => command.InstallationId == cleanInstallationId)
                .Where(command => command.Status == ControlCloudInstallationCommandStatuses.Pending)
                .Where(command => command.ExpiresAtUtc > asOfUtc)
                .Where(command => command.NotBeforeUtc is null || command.NotBeforeUtc <= asOfUtc)
                .OrderBy(command => command.CommandVersion)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ControlCloudInstallationCommand?> GetLatestByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        var cleanInstallationId = installationId.Trim();

        await _gate.WaitAsync(cancellationToken);

        try
        {
            return (await ReadAllAsync(cancellationToken))
                .Where(command => command.InstallationId == cleanInstallationId)
                .OrderBy(command => command.CommandVersion)
                .ThenBy(command => command.CommandId)
                .LastOrDefault();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(
        ControlCloudInstallationCommand command,
        CancellationToken cancellationToken = default)
    {
        await SaveAsync(command, cancellationToken);
    }

    public async Task SaveAsync(
        ControlCloudInstallationCommand command,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var commands = await ReadAllAsync(cancellationToken);
            var existingIndex = commands.FindIndex(stored => stored.CommandId == command.CommandId);

            if (existingIndex >= 0)
            {
                commands[existingIndex] = command;
            }
            else
            {
                commands.Add(command);
            }

            await WriteAllAsync(commands, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<ControlCloudInstallationCommand>> ReadAllAsync(
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
        var records = await JsonSerializer.DeserializeAsync<List<CommandRecord>>(
            stream,
            JsonOptions,
            cancellationToken);

        return (records ?? [])
            .Select(record => ControlCloudInstallationCommand.Restore(
                record.CommandId,
                record.ClientId,
                record.InstallationId,
                record.CommandVersion,
                record.CommandType,
                record.Status,
                record.IdempotencyKey,
                record.PayloadJson,
                record.SignatureAlgorithm,
                record.SignatureKeyId,
                record.PayloadSha256,
                record.SignatureValue,
                record.QueuedAtUtc,
                record.NotBeforeUtc,
                record.ExpiresAtUtc,
                record.AcknowledgedAtUtc,
                record.AcknowledgementStatus,
                record.AcknowledgementDetail))
            .ToList();
    }

    private async Task WriteAllAsync(
        IReadOnlyCollection<ControlCloudInstallationCommand> commands,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_storePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var records = commands
            .OrderBy(command => command.InstallationId, StringComparer.Ordinal)
            .ThenBy(command => command.CommandVersion)
            .Select(command => new CommandRecord(
                command.CommandId,
                command.ClientId,
                command.InstallationId,
                command.CommandVersion,
                command.CommandType,
                command.Status,
                command.IdempotencyKey,
                command.PayloadJson,
                command.SignatureAlgorithm,
                command.SignatureKeyId,
                command.PayloadSha256,
                command.SignatureValue,
                command.QueuedAtUtc,
                command.NotBeforeUtc,
                command.ExpiresAtUtc,
                command.AcknowledgedAtUtc,
                command.AcknowledgementStatus,
                command.AcknowledgementDetail))
            .ToArray();

        await using var stream = new FileStream(
            _storePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, records, JsonOptions, cancellationToken);
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/control-cloud-installation-commands.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private sealed record CommandRecord(
        Guid CommandId,
        Guid ClientId,
        string InstallationId,
        long CommandVersion,
        string CommandType,
        string Status,
        string IdempotencyKey,
        string PayloadJson,
        string SignatureAlgorithm,
        string SignatureKeyId,
        string PayloadSha256,
        string SignatureValue,
        DateTimeOffset QueuedAtUtc,
        DateTimeOffset? NotBeforeUtc,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset? AcknowledgedAtUtc,
        string? AcknowledgementStatus,
        string? AcknowledgementDetail);
}
