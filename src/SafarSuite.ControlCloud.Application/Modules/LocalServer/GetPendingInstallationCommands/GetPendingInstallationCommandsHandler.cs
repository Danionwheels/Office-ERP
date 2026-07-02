using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.GetPendingInstallationCommands;

public sealed class GetPendingInstallationCommandsHandler
{
    private readonly IControlCloudClientInstallationRepository _installations;
    private readonly IControlCloudInstallationCommandRepository _commands;
    private readonly IControlCloudClock _clock;

    public GetPendingInstallationCommandsHandler(
        IControlCloudClientInstallationRepository installations,
        IControlCloudInstallationCommandRepository commands,
        IControlCloudClock clock)
    {
        _installations = installations;
        _commands = commands;
        _clock = clock;
    }

    public async Task<GetPendingInstallationCommandsResult> HandleAsync(
        GetPendingInstallationCommandsQuery query,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeInstallationId(query.InstallationId);

        if (installationId is null)
        {
            return GetPendingInstallationCommandsResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before listing pending commands.");
        }

        var installation = await _installations.GetByInstallationIdAsync(
            installationId,
            cancellationToken);

        if (installation is null)
        {
            return GetPendingInstallationCommandsResult.Failure(
                "InstallationNotFound",
                "Installation is not registered.");
        }

        var commands = await _commands.ListPendingAsync(
            installationId,
            _clock.UtcNow,
            cancellationToken);

        return GetPendingInstallationCommandsResult.Success(commands);
    }

    private static string? NormalizeInstallationId(string installationId)
    {
        var normalized = installationId.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
