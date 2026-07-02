using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface IControlCloudInstallationProvisioningClient
{
    Task<ControlCloudSetupTokenClientResult> CreateSetupTokenAsync(
        Guid clientId,
        string installationId,
        CreateLocalServerSetupTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<ControlCloudBootstrapPackageClientResult> CreateBootstrapPackageAsync(
        Guid clientId,
        string installationId,
        CreateLocalServerBootstrapPackageRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ControlCloudSetupTokenClientResult
{
    private ControlCloudSetupTokenClientResult(
        LocalServerSetupTokenResponse? setupToken,
        string? failureCode,
        string? detail)
    {
        SetupToken = setupToken;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => SetupToken is not null;

    public LocalServerSetupTokenResponse? SetupToken { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudSetupTokenClientResult Success(
        LocalServerSetupTokenResponse setupToken)
    {
        return new ControlCloudSetupTokenClientResult(
            setupToken,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudSetupTokenClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudSetupTokenClientResult(
            setupToken: null,
            failureCode,
            detail);
    }
}

public sealed class ControlCloudBootstrapPackageClientResult
{
    private ControlCloudBootstrapPackageClientResult(
        LocalServerBootstrapPackageResponse? bootstrapPackage,
        string? failureCode,
        string? detail)
    {
        BootstrapPackage = bootstrapPackage;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => BootstrapPackage is not null;

    public LocalServerBootstrapPackageResponse? BootstrapPackage { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudBootstrapPackageClientResult Success(
        LocalServerBootstrapPackageResponse bootstrapPackage)
    {
        return new ControlCloudBootstrapPackageClientResult(
            bootstrapPackage,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudBootstrapPackageClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudBootstrapPackageClientResult(
            bootstrapPackage: null,
            failureCode,
            detail);
    }
}
