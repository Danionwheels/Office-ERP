using SafarSuite.ControlDesk.Contracts.SafarSuiteApp.V1;

namespace SafarSuite.ControlDesk.Application.Modules.Contracts.Ports;

public interface IProductKernelCommandIssuerClient
{
    Task<ProductKernelCommandIssueClientResult> IssueCommandAsync(
        Guid activationRequestId,
        IssueProductKernelCommandRequest request,
        string requestedBy,
        CancellationToken cancellationToken = default);
}

public sealed class ProductKernelCommandIssueClientResult
{
    private ProductKernelCommandIssueClientResult(
        CloudProductKernelCommandResponse? command,
        string? failureCode,
        string? detail)
    {
        Command = command;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Command is not null;

    public CloudProductKernelCommandResponse? Command { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ProductKernelCommandIssueClientResult Success(
        CloudProductKernelCommandResponse command)
    {
        return new ProductKernelCommandIssueClientResult(command, null, null);
    }

    public static ProductKernelCommandIssueClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ProductKernelCommandIssueClientResult(null, failureCode, detail);
    }
}
