using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.UpdateProviderAccessOperatorScopes;

public sealed class UpdateProviderAccessOperatorScopesHandler
{
    private readonly IControlCloudProviderAccessClient _providerAccess;

    public UpdateProviderAccessOperatorScopesHandler(
        IControlCloudProviderAccessClient providerAccess)
    {
        _providerAccess = providerAccess;
    }

    public async Task<Result<ProviderAccessOperatorResponse>> HandleAsync(
        UpdateProviderAccessOperatorScopesCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ProviderAccessOperatorAdminValidator.ValidateScopes(
            command.UserId,
            command.Scopes,
            command.UpdatedBy);

        if (validationErrors.Count > 0)
        {
            return Result<ProviderAccessOperatorResponse>.Failure(validationErrors);
        }

        var request = new UpdateProviderOperatorScopesRequest(
            ProviderAccessOperatorAdminValidator.NormalizeScopes(command.Scopes),
            ProviderAccessOperatorAdminValidator.NormalizeActor(command.UpdatedBy));
        var result = await _providerAccess.UpdateOperatorScopesAsync(
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
