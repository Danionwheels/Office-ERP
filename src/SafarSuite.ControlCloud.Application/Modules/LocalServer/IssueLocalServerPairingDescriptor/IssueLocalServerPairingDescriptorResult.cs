using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueLocalServerPairingDescriptor;

public sealed record IssueLocalServerPairingDescriptorResult(
    LocalServerPairingDescriptorResponse? Descriptor,
    string? FailureCode,
    string? Detail)
{
    public bool IsSuccess => Descriptor is not null;

    public static IssueLocalServerPairingDescriptorResult Success(
        LocalServerPairingDescriptorResponse descriptor)
    {
        return new IssueLocalServerPairingDescriptorResult(
            descriptor,
            FailureCode: null,
            Detail: null);
    }

    public static IssueLocalServerPairingDescriptorResult Failure(
        string failureCode,
        string detail)
    {
        return new IssueLocalServerPairingDescriptorResult(
            Descriptor: null,
            failureCode,
            detail);
    }
}
