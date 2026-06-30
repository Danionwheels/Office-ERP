namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Common;

public sealed record ApiErrorResponse(
    int StatusCode,
    string Title,
    IReadOnlyCollection<ApiErrorItem> Errors);

public sealed record ApiErrorItem(
    string Code,
    string Message,
    string? Target);
