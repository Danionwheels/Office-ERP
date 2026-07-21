namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record CompleteClientPortalPasswordResetResult(
    bool IsSuccess,
    string? FailureCode,
    string? Detail)
{
    public static CompleteClientPortalPasswordResetResult Success() => new(true, null, null);
    public static CompleteClientPortalPasswordResetResult Failure(string code, string detail) => new(false, code, detail);
}
