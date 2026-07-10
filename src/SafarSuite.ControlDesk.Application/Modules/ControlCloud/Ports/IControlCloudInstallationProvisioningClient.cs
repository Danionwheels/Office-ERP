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

    Task<ControlCloudBootstrapPackageRegisterClientResult> ListBootstrapPackagesAsync(
        Guid clientId,
        string installationId,
        int take,
        CancellationToken cancellationToken = default);

    Task<ControlCloudBootstrapPackageHandoffClientResult> MarkBootstrapPackageHandoffAsync(
        Guid clientId,
        string installationId,
        Guid bootstrapPackageId,
        MarkLocalServerBootstrapPackageHandoffRequest request,
        CancellationToken cancellationToken = default);

    Task<ControlCloudAppActivationTokenClientResult> IssueAppActivationTokenAsync(
        Guid clientId,
        string installationId,
        IssueSafarSuiteAppActivationTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<ControlCloudFirstManagerSetupTokenClientResult> IssueFirstManagerSetupTokenAsync(
        Guid clientId,
        string installationId,
        IssueLocalServerFirstManagerSetupTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<ControlCloudPairingDescriptorClientResult> IssuePairingDescriptorAsync(
        Guid clientId,
        string installationId,
        IssueLocalServerPairingDescriptorRequest request,
        CancellationToken cancellationToken = default);

    Task<ControlCloudAppActivationIssuesClientResult> ListAppActivationIssuesAsync(
        Guid clientId,
        string? installationId,
        Guid? appServerInstallationId,
        string? query,
        int take,
        CancellationToken cancellationToken = default);

    Task<ControlCloudAppActivationIssueClientResult> RevokeAppActivationIssueAsync(
        Guid clientId,
        Guid activationIssueId,
        RevokeSafarSuiteAppActivationIssueRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ControlCloudPairingDescriptorClientResult
{
    private ControlCloudPairingDescriptorClientResult(
        LocalServerPairingDescriptorResponse? descriptor,
        string? failureCode,
        string? detail)
    {
        Descriptor = descriptor;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Descriptor is not null;

    public LocalServerPairingDescriptorResponse? Descriptor { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudPairingDescriptorClientResult Success(
        LocalServerPairingDescriptorResponse descriptor)
    {
        return new ControlCloudPairingDescriptorClientResult(
            descriptor,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudPairingDescriptorClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudPairingDescriptorClientResult(
            descriptor: null,
            failureCode,
            detail);
    }
}

public sealed class ControlCloudBootstrapPackageHandoffClientResult
{
    private ControlCloudBootstrapPackageHandoffClientResult(
        LocalServerBootstrapPackageHandoffResponse? response,
        string? failureCode,
        string? detail)
    {
        Response = response;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Response is not null;

    public LocalServerBootstrapPackageHandoffResponse? Response { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudBootstrapPackageHandoffClientResult Success(
        LocalServerBootstrapPackageHandoffResponse response)
    {
        return new ControlCloudBootstrapPackageHandoffClientResult(
            response,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudBootstrapPackageHandoffClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudBootstrapPackageHandoffClientResult(
            response: null,
            failureCode,
            detail);
    }
}

public sealed class ControlCloudFirstManagerSetupTokenClientResult
{
    private ControlCloudFirstManagerSetupTokenClientResult(
        IssueLocalServerFirstManagerSetupTokenResponse? response,
        string? failureCode,
        string? detail)
    {
        Response = response;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Response is not null;

    public IssueLocalServerFirstManagerSetupTokenResponse? Response { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudFirstManagerSetupTokenClientResult Success(
        IssueLocalServerFirstManagerSetupTokenResponse response)
    {
        return new ControlCloudFirstManagerSetupTokenClientResult(
            response,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudFirstManagerSetupTokenClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudFirstManagerSetupTokenClientResult(
            response: null,
            failureCode,
            detail);
    }
}

public sealed class ControlCloudAppActivationIssueClientResult
{
    private ControlCloudAppActivationIssueClientResult(
        SafarSuiteAppActivationIssueResponse? issue,
        string? failureCode,
        string? detail)
    {
        Issue = issue;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Issue is not null;

    public SafarSuiteAppActivationIssueResponse? Issue { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudAppActivationIssueClientResult Success(
        SafarSuiteAppActivationIssueResponse issue)
    {
        return new ControlCloudAppActivationIssueClientResult(
            issue,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudAppActivationIssueClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudAppActivationIssueClientResult(
            issue: null,
            failureCode,
            detail);
    }
}

public sealed class ControlCloudAppActivationIssuesClientResult
{
    private ControlCloudAppActivationIssuesClientResult(
        SafarSuiteAppActivationIssuesResponse? response,
        string? failureCode,
        string? detail)
    {
        Response = response;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Response is not null;

    public SafarSuiteAppActivationIssuesResponse? Response { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudAppActivationIssuesClientResult Success(
        SafarSuiteAppActivationIssuesResponse response)
    {
        return new ControlCloudAppActivationIssuesClientResult(
            response,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudAppActivationIssuesClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudAppActivationIssuesClientResult(
            response: null,
            failureCode,
            detail);
    }
}

public sealed class ControlCloudAppActivationTokenClientResult
{
    private ControlCloudAppActivationTokenClientResult(
        IssueSafarSuiteAppActivationTokenResponse? response,
        string? failureCode,
        string? detail)
    {
        Response = response;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Response is not null;

    public IssueSafarSuiteAppActivationTokenResponse? Response { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudAppActivationTokenClientResult Success(
        IssueSafarSuiteAppActivationTokenResponse response)
    {
        return new ControlCloudAppActivationTokenClientResult(
            response,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudAppActivationTokenClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudAppActivationTokenClientResult(
            response: null,
            failureCode,
            detail);
    }
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

public sealed class ControlCloudBootstrapPackageRegisterClientResult
{
    private ControlCloudBootstrapPackageRegisterClientResult(
        LocalServerBootstrapPackageRegisterResponse? response,
        string? failureCode,
        string? detail)
    {
        Response = response;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Response is not null;

    public LocalServerBootstrapPackageRegisterResponse? Response { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ControlCloudBootstrapPackageRegisterClientResult Success(
        LocalServerBootstrapPackageRegisterResponse response)
    {
        return new ControlCloudBootstrapPackageRegisterClientResult(
            response,
            failureCode: null,
            detail: null);
    }

    public static ControlCloudBootstrapPackageRegisterClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ControlCloudBootstrapPackageRegisterClientResult(
            response: null,
            failureCode,
            detail);
    }
}
