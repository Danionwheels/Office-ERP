namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record ConfirmClientPortalMfaEnrollmentResult(
    bool IsSuccess,
    CreateClientPortalSessionResult? Session,
    string? FailureCode,
    string? Detail)
{
    public static ConfirmClientPortalMfaEnrollmentResult Success(CreateClientPortalSessionResult session) =>
        new(true, session, null, null);
    public static ConfirmClientPortalMfaEnrollmentResult Failure(string code, string detail) =>
        new(false, null, code, detail);
}
