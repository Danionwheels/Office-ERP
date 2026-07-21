namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record BeginClientPortalMfaEnrollmentResult(
    bool IsSuccess,
    string? Secret,
    string? OtpAuthUri,
    string? QrCodeDataUri,
    IReadOnlyCollection<string> RecoveryCodes,
    string? FailureCode,
    string? Detail)
{
    public static BeginClientPortalMfaEnrollmentResult Success(
        string secret,
        string otpAuthUri,
        string qrCodeDataUri,
        IReadOnlyCollection<string> recoveryCodes) =>
        new(true, secret, otpAuthUri, qrCodeDataUri, recoveryCodes, null, null);

    public static BeginClientPortalMfaEnrollmentResult Failure(string code, string detail) =>
        new(false, null, null, null, [], code, detail);
}
