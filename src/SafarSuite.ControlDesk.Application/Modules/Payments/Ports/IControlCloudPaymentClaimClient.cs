using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.Ports;

public interface IControlCloudPaymentClaimClient
{
    Task<ControlCloudPaymentClaimListClientResult> ListAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<ControlCloudPaymentClaimProofClientResult> GetProofAsync(
        Guid claimId,
        CancellationToken cancellationToken = default);
}

public sealed record ControlCloudPaymentClaimListClientResult(
    bool IsSuccess,
    IReadOnlyCollection<ClientPortalPaymentClaimResponse> Claims,
    string? FailureCode,
    string? Detail)
{
    public static ControlCloudPaymentClaimListClientResult Success(
        IReadOnlyCollection<ClientPortalPaymentClaimResponse> claims)
    {
        return new ControlCloudPaymentClaimListClientResult(true, claims, null, null);
    }

    public static ControlCloudPaymentClaimListClientResult Failure(string failureCode, string detail)
    {
        return new ControlCloudPaymentClaimListClientResult(
            false,
            Array.Empty<ClientPortalPaymentClaimResponse>(),
            failureCode,
            detail);
    }
}

public sealed record ControlCloudPaymentClaimProofClientResult(
    bool IsSuccess,
    byte[] Content,
    string ContentType,
    string FileName,
    string? FailureCode,
    string? Detail)
{
    public static ControlCloudPaymentClaimProofClientResult Success(
        byte[] content,
        string contentType,
        string fileName)
    {
        return new ControlCloudPaymentClaimProofClientResult(
            true,
            content,
            contentType,
            fileName,
            null,
            null);
    }

    public static ControlCloudPaymentClaimProofClientResult Failure(string failureCode, string detail)
    {
        return new ControlCloudPaymentClaimProofClientResult(
            false,
            Array.Empty<byte>(),
            "application/octet-stream",
            "payment-proof",
            failureCode,
            detail);
    }
}
