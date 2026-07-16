namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IClientPortalTotpService
{
    string CreateSecret();

    string CreateOtpAuthUri(
        string issuer,
        string accountName,
        string secret);

    string CreateQrCodeSvg(string value);

    string CreateQrCodeDataUri(string value);

    bool TryVerifyCode(
        string secret,
        string? code,
        DateTimeOffset now,
        long? lastAcceptedStep,
        out long acceptedStep);
}
