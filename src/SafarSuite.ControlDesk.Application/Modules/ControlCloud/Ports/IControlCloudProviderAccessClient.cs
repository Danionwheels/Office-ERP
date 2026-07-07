using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface IControlCloudProviderAccessClient
{
    Task<ControlCloudProviderOperatorsClientResult> ListOperatorsAsync(
        CancellationToken cancellationToken = default);

    Task<ControlCloudProviderOperatorClientResult> CreateOperatorAsync(
        CreateProviderOperatorRequest request,
        CancellationToken cancellationToken = default);

    Task<ControlCloudProviderOperatorClientResult> ResetOperatorPasswordAsync(
        string userId,
        ResetProviderOperatorPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<ControlCloudProviderOperatorClientResult> UpdateOperatorScopesAsync(
        string userId,
        UpdateProviderOperatorScopesRequest request,
        CancellationToken cancellationToken = default);

    Task<ControlCloudProviderOperatorClientResult> UpdateOperatorStatusAsync(
        string userId,
        UpdateProviderOperatorStatusRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ControlCloudProviderOperatorsClientResult
{
    private ControlCloudProviderOperatorsClientResult(
        ProviderAccessOperatorsResponse? response,
        string? failureCode,
        string? detail)
    {
        Response = response;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Response is not null;

    public ProviderAccessOperatorsResponse? Response { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudProviderOperatorsClientResult Success(
        ProviderAccessOperatorsResponse response)
    {
        return new ControlCloudProviderOperatorsClientResult(
            response,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudProviderOperatorsClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudProviderOperatorsClientResult(
            response: null,
            failureCode,
            detail);
    }
}

public sealed class ControlCloudProviderOperatorClientResult
{
    private ControlCloudProviderOperatorClientResult(
        ProviderAccessOperatorResponse? providerOperator,
        string? failureCode,
        string? detail)
    {
        Operator = providerOperator;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Operator is not null;

    public ProviderAccessOperatorResponse? Operator { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudProviderOperatorClientResult Success(
        ProviderAccessOperatorResponse providerOperator)
    {
        return new ControlCloudProviderOperatorClientResult(
            providerOperator,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudProviderOperatorClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudProviderOperatorClientResult(
            providerOperator: null,
            failureCode,
            detail);
    }
}
