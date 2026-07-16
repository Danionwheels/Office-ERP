namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalMfaSecretProtector
{
    string Protect(string secret);

    bool TryUnprotect(string protectedSecret, out string secret);
}
