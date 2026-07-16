namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalCredentialService
{
    string CreateInvitationToken();

    string CreateSecureToken(int byteCount = 32);

    IReadOnlyCollection<string> CreateRecoveryCodes(int count = 10);

    string NormalizeRecoveryCode(string recoveryCode);

    string HashSecret(string secret);

    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}
