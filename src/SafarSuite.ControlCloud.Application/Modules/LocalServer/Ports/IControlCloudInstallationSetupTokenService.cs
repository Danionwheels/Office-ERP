namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

public interface IControlCloudInstallationSetupTokenService
{
    string CreateSetupToken();

    string HashSecret(string secret);
}
