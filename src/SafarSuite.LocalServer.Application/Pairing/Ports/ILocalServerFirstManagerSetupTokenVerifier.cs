using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Pairing.Ports;

public interface ILocalServerFirstManagerSetupTokenVerifier
{
    LocalServerFirstManagerSetupTokenVerificationResult Verify(
        LocalServerSignedFirstManagerSetupTokenResponse? token,
        DateTimeOffset importedAtUtc);
}

public sealed class LocalServerFirstManagerSetupTokenVerificationResult
{
    private LocalServerFirstManagerSetupTokenVerificationResult(
        LocalServerFirstManagerSetupTokenPayloadResponse? payload,
        LocalServerBootstrapPackageSignatureResponse? signature,
        string? failureCode,
        string? detail)
    {
        Payload = payload;
        Signature = signature;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Payload is not null && Signature is not null;

    public LocalServerFirstManagerSetupTokenPayloadResponse? Payload { get; }

    public LocalServerBootstrapPackageSignatureResponse? Signature { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static LocalServerFirstManagerSetupTokenVerificationResult Success(
        LocalServerFirstManagerSetupTokenPayloadResponse payload,
        LocalServerBootstrapPackageSignatureResponse signature)
    {
        return new LocalServerFirstManagerSetupTokenVerificationResult(
            payload,
            signature,
            null,
            null);
    }

    public static LocalServerFirstManagerSetupTokenVerificationResult Failure(
        string failureCode,
        string detail)
    {
        return new LocalServerFirstManagerSetupTokenVerificationResult(
            null,
            null,
            failureCode,
            detail);
    }
}
