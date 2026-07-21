using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateProviderAccessOperator;

public sealed class CreateProviderAccessOperatorHandler
{
    private readonly IControlCloudProviderAccessClient _providerAccess;

    public CreateProviderAccessOperatorHandler(
        IControlCloudProviderAccessClient providerAccess)
    {
        _providerAccess = providerAccess;
    }

    public async Task<Result<ProviderAccessOperatorResponse>> HandleAsync(
        CreateProviderAccessOperatorCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ProviderAccessOperatorAdminValidator.ValidateCreate(
            command.Email,
            command.FullName,
            command.Password,
            command.Scopes,
            command.CreatedBy);

        if (validationErrors.Count > 0)
        {
            return Result<ProviderAccessOperatorResponse>.Failure(validationErrors);
        }

        var request = new CreateProviderOperatorRequest(
            command.Email.Trim(),
            command.FullName.Trim(),
            command.Password,
            ProviderAccessOperatorAdminValidator.NormalizeScopes(command.Scopes),
            ProviderAccessOperatorAdminValidator.NormalizeActor(command.CreatedBy));
        var result = await _providerAccess.CreateOperatorAsync(request, cancellationToken);

        return result.IsSuccess
            ? Result<ProviderAccessOperatorResponse>.Success(result.Operator!)
            : Result<ProviderAccessOperatorResponse>.Failure(
                ProviderAccessOperatorAdminValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }
}
