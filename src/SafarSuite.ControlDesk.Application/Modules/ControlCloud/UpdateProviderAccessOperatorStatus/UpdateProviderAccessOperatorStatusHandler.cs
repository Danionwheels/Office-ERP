using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.UpdateProviderAccessOperatorStatus;

public sealed class UpdateProviderAccessOperatorStatusHandler
{
    private readonly IControlCloudProviderAccessClient _providerAccess;

    public UpdateProviderAccessOperatorStatusHandler(
        IControlCloudProviderAccessClient providerAccess)
    {
        _providerAccess = providerAccess;
    }

    public async Task<Result<ProviderAccessOperatorResponse>> HandleAsync(
        UpdateProviderAccessOperatorStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ProviderAccessOperatorAdminValidator.ValidateStatus(
            command.UserId,
            command.Status,
            command.UpdatedBy);

        if (validationErrors.Count > 0)
        {
            return Result<ProviderAccessOperatorResponse>.Failure(validationErrors);
        }

        var request = new UpdateProviderOperatorStatusRequest(
            command.Status.Trim(),
            ProviderAccessOperatorAdminValidator.NormalizeActor(command.UpdatedBy));
        var result = await _providerAccess.UpdateOperatorStatusAsync(
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
