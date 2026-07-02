using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.RegisterLocalServerInstallation;

public sealed record RegisterLocalServerInstallationResult(
    ControlCloudClientInstallation? Installation,
    string LocalServerVersion,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Installation is not null;

    public static RegisterLocalServerInstallationResult Success(
        ControlCloudClientInstallation installation,
        string localServerVersion)
    {
        return new RegisterLocalServerInstallationResult(
            installation,
            localServerVersion,
            FailureCode: null,
            Detail: null);
    }

    public static RegisterLocalServerInstallationResult Failure(
        string failureCode,
        string detail)
    {
        return new RegisterLocalServerInstallationResult(
            Installation: null,
            LocalServerVersion: "",
            failureCode,
            detail);
    }
}
