namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record ClientPortalPaymentOperationResult<T>(
    T? Value,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => FailureCode is null;

    public static ClientPortalPaymentOperationResult<T> Success(T value) =>
        new(value, null, null);

    public static ClientPortalPaymentOperationResult<T> Failure(
        string failureCode,
        string detail) =>
        new(default, failureCode, detail);
}
