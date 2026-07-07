using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ResetProviderAccessOperatorPassword;

public sealed class ResetProviderAccessOperatorPasswordHandler
{
    private readonly IControlCloudProviderAccessClient _providerAccess;

    public ResetProviderAccessOperatorPasswordHandler(
        IControlCloudProviderAccessClient providerAccess)
    {
        _providerAccess = providerAccess;
    }

    public async Task<Result<ProviderAccessOperatorResponse>> HandleAsync(
        ResetProviderAccessOperatorPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ProviderAccessOperatorAdminValidator.ValidatePasswordReset(
            command.UserId,
            command.Password,
            command.UpdatedBy);

        if (validationErrors.Count > 0)
        {
            return Result<ProviderAccessOperatorResponse>.Failure(validationErrors);
        }

        var request = new ResetProviderOperatorPasswordRequest(
            command.Password,
            ProviderAccessOperatorAdminValidator.NormalizeActor(command.UpdatedBy));
        var result = await _providerAccess.ResetOperatorPasswordAsync(
            command.UserId.Trim(),
            request,
            cancellationToken);

        return result.IsSuccess
            ? Result<ProviderAccessOperatorResponse>.Success(result.Operator!)
            : Result<ProviderAccessOperatorResponse>.Failure(
                ProviderAccessOperatorAdminValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }
}
