using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileClientPortalInvitationDeliveryRecorder : IClientPortalInvitationDeliveryRecorder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileClientPortalInvitationDeliveryRecorder(ClientPortalInvitationDeliveryOptions options)
    {
        _storePath = ResolveStorePath(options.StorePath);
    }

    public async Task RecordAsync(
        ClientPortalInvitationDeliveryRecord delivery,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var directory = Path.GetDirectoryName(_storePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = JsonSerializer.Serialize(delivery, JsonOptions);
            await File.AppendAllTextAsync(
                _storePath,
                line + Environment.NewLine,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/client-portal-invitation-deliveries.jsonl"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
