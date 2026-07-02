using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.LocalServer.Application.Commands.Ports;

public interface ILocalServerInstallationCommandVerifier
{
    LocalServerInstallationCommandVerificationResult Verify(
        InstallationCommandResponse command,
        Guid expectedClientId,
        string expectedInstallationId);
}

public sealed record LocalServerInstallationCommandVerificationResult(
    bool IsSuccess,
    string? FailureCode,
    string? Detail)
{
    public static LocalServerInstallationCommandVerificationResult Success()
    {
        return new LocalServerInstallationCommandVerificationResult(
            true,
            FailureCode: null,
            Detail: null);
    }

    public static LocalServerInstallationCommandVerificationResult Failure(
        string failureCode,
        string detail)
    {
        return new LocalServerInstallationCommandVerificationResult(
            false,
            failureCode,
            detail);
    }
}
