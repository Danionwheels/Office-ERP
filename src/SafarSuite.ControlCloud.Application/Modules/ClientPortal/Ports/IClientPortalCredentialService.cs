namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalCredentialService
{
    string CreateInvitationToken();

    string HashSecret(string secret);

    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}
