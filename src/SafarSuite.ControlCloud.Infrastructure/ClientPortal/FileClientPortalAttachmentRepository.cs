using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileClientPortalAttachmentRepository : IClientPortalAttachmentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileClientPortalAttachmentRepository(ClientPortalAccessOptions options) =>
        _storePath = Resolve(options.AttachmentStorePath);

    public async Task<ControlCloudClientPortalAttachment?> GetByIdAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            return (await ReadAsync(cancellationToken))
                .SingleOrDefault(attachment => attachment.AttachmentId == attachmentId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(
        ControlCloudClientPortalAttachment attachment,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var attachments = await ReadAsync(cancellationToken);
            if (attachments.Any(stored => stored.AttachmentId == attachment.AttachmentId))
            {
                throw new InvalidOperationException(
                    $"Client Portal attachment '{attachment.AttachmentId}' already exists.");
            }

            attachments.Add(attachment);
            await WriteAsync(attachments, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<ControlCloudClientPortalAttachment>> ReadAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_storePath);
        return await JsonSerializer.DeserializeAsync<List<ControlCloudClientPortalAttachment>>(
            stream,
            JsonOptions,
            cancellationToken) ?? [];
    }

    private async Task WriteAsync(
        List<ControlCloudClientPortalAttachment> attachments,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
        await using var stream = new FileStream(
            _storePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, attachments, JsonOptions, cancellationToken);
    }

    private static string Resolve(string path) => Path.IsPathRooted(path)
        ? path
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
}
