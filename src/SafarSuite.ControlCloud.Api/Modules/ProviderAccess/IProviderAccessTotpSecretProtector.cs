namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public interface IProviderAccessTotpSecretProtector
{
    string Protect(string secret);

    bool TryUnprotect(
        string storedSecret,
        out string secret);

    bool IsProtected(string storedSecret);
}
