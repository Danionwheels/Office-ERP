using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ResetProviderAccessOperatorTotp;

public sealed class ResetProviderAccessOperatorTotpHandler
{
    private readonly IControlCloudProviderAccessClient _providerAccess;

    public ResetProviderAccessOperatorTotpHandler(
        IControlCloudProviderAccessClient providerAccess)
    {
        _providerAccess = providerAccess;
    }

    public async Task<Result<ProviderOperatorTotpEnrollmentResponse>> HandleAsync(
        ResetProviderAccessOperatorTotpCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ProviderAccessOperatorAdminValidator.ValidateTotpReset(
            command.UserId,
            command.UpdatedBy);

        if (validationErrors.Count > 0)
        {
            return Result<ProviderOperatorTotpEnrollmentResponse>.Failure(validationErrors);
        }

        var request = new ResetProviderOperatorTotpRequest(
            ProviderAccessOperatorAdminValidator.NormalizeActor(command.UpdatedBy));
        var result = await _providerAccess.ResetOperatorTotpAsync(
            command.UserId.Trim(),
            request,
            cancellationToken);

        return result.IsSuccess
            ? Result<ProviderOperatorTotpEnrollmentResponse>.Success(result.Response!)
            : Result<ProviderOperatorTotpEnrollmentResponse>.Failure(
                ProviderAccessOperatorAdminValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }
}
