using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ResetProviderAccessOperatorRecoveryCodes;

public sealed class ResetProviderAccessOperatorRecoveryCodesHandler
{
    private readonly IControlCloudProviderAccessClient _providerAccess;

    public ResetProviderAccessOperatorRecoveryCodesHandler(
        IControlCloudProviderAccessClient providerAccess)
    {
        _providerAccess = providerAccess;
    }

    public async Task<Result<ProviderOperatorRecoveryCodesResponse>> HandleAsync(
        ResetProviderAccessOperatorRecoveryCodesCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ProviderAccessOperatorAdminValidator.ValidateRecoveryCodeReset(
            command.UserId,
            command.Count,
            command.UpdatedBy);

        if (validationErrors.Count > 0)
        {
            return Result<ProviderOperatorRecoveryCodesResponse>.Failure(validationErrors);
        }

        var request = new ResetProviderOperatorRecoveryCodesRequest(
            command.Count,
            ProviderAccessOperatorAdminValidator.NormalizeActor(command.UpdatedBy));
        var result = await _providerAccess.ResetOperatorRecoveryCodesAsync(
            command.UserId.Trim(),
            request,
            cancellationToken);

        return result.IsSuccess
            ? Result<ProviderOperatorRecoveryCodesResponse>.Success(result.Response!)
            : Result<ProviderOperatorRecoveryCodesResponse>.Failure(
                ProviderAccessOperatorAdminValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }
}
