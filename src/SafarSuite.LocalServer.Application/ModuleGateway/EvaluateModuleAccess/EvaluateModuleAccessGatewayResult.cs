using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.ModuleGateway.EvaluateModuleAccess;

public sealed class EvaluateModuleAccessGatewayResult
{
    private EvaluateModuleAccessGatewayResult(
        LocalServerModuleAccessResponse? access,
        string? failureCode,
        string? detail)
    {
        Access = access;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Access is not null;

    public LocalServerModuleAccessResponse? Access { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static EvaluateModuleAccessGatewayResult Success(
        LocalServerModuleAccessResponse access)
    {
        return new EvaluateModuleAccessGatewayResult(access, null, null);
    }

    public static EvaluateModuleAccessGatewayResult Failure(
        string failureCode,
        string detail)
    {
        return new EvaluateModuleAccessGatewayResult(null, failureCode, detail);
    }
}
