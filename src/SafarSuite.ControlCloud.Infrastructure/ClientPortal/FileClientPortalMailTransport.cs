using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileClientPortalMailTransport : IClientPortalMailTransport
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path;

    public FileClientPortalMailTransport(ClientPortalInvitationDeliveryOptions options)
    {
        var configured = string.IsNullOrWhiteSpace(options.StorePath)
            ? "App_Data/client-portal-mail-deliveries.jsonl"
            : options.StorePath.Trim();
        _path = Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configured));
    }

    public async Task SendAsync(ClientPortalMailMessage message, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            await File.AppendAllTextAsync(
                _path,
                JsonSerializer.Serialize(message) + Environment.NewLine,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}
