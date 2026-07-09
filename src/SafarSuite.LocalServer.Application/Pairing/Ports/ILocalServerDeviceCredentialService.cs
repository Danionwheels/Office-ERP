using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Domain.Pairing;

namespace SafarSuite.LocalServer.Application.Pairing.Ports;

public interface ILocalServerDeviceCredentialService
{
    LocalServerSignedDeviceCredentialResponse Issue(
        LocalServerDevicePairingRecord device,
        Guid credentialId,
        string assignedRole,
        DateTimeOffset issuedAtUtc);

    LocalServerDeviceCredentialVerificationResult Verify(
        string? compactToken,
        DateTimeOffset verifiedAtUtc);
}

public sealed record LocalServerDeviceCredentialVerificationResult(
    bool IsSuccess,
    LocalServerDeviceCredentialPayloadResponse? Payload,
    LocalServerBootstrapPackageSignatureResponse? Signature,
    string? CompactToken,
    string? FailureCode,
    string? Detail)
{
    public static LocalServerDeviceCredentialVerificationResult Success(
        LocalServerDeviceCredentialPayloadResponse payload,
        LocalServerBootstrapPackageSignatureResponse signature,
        string compactToken)
    {
        return new LocalServerDeviceCredentialVerificationResult(
            true,
            payload,
            signature,
            compactToken,
            null,
            null);
    }

    public static LocalServerDeviceCredentialVerificationResult Failure(
        string failureCode,
        string detail)
    {
        return new LocalServerDeviceCredentialVerificationResult(
            false,
            null,
            null,
            null,
            failureCode,
            detail);
    }
}
