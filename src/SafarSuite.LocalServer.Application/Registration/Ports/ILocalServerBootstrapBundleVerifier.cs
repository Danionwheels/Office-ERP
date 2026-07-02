using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Domain.Registration;

namespace SafarSuite.LocalServer.Application.Registration.Ports;

public interface ILocalServerBootstrapBundleVerifier
{
    LocalServerBootstrapBundleVerificationResult Verify(
        LocalServerSignedBootstrapBundleResponse bundle,
        DateTimeOffset importedAtUtc,
        string? expectedInstallationId = null);
}

public sealed class LocalServerBootstrapBundleVerificationResult
{
    private LocalServerBootstrapBundleVerificationResult(
        LocalServerBootstrapConfiguration? configuration,
        string? failureCode,
        string? detail)
    {
        Configuration = configuration;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsValid => Configuration is not null;

    public LocalServerBootstrapConfiguration? Configuration { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static LocalServerBootstrapBundleVerificationResult Success(
        LocalServerBootstrapConfiguration configuration)
    {
        return new LocalServerBootstrapBundleVerificationResult(
            configuration,
            failureCode: null,
            detail: null);
    }

    public static LocalServerBootstrapBundleVerificationResult Failure(
        string failureCode,
        string detail)
    {
        return new LocalServerBootstrapBundleVerificationResult(
            configuration: null,
            failureCode,
            detail);
    }
}
