using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Common;

internal static class ControlCloudAuditWriter
{
    public static async Task TryRecordAsync(
        IClientPortalAuditRecorder audit,
        ClientPortalAuditRecord record,
        CancellationToken cancellationToken)
    {
        try
        {
            await audit.RecordAsync(record, cancellationToken);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static string NormalizeActor(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }

    public static string NormalizeEmail(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim().ToLowerInvariant();
    }
}
