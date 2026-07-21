using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ChangeProviderAccessOperatorPassword;

public sealed class ChangeProviderAccessOperatorPasswordHandler
{
    private readonly IControlCloudProviderAccessClient _client;

    public ChangeProviderAccessOperatorPasswordHandler(
        IControlCloudProviderAccessClient client)
    {
        _client = client;
    }

    public async Task<Result<ProviderAccessOperatorResponse>> HandleAsync(
        ChangeProviderAccessOperatorPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var errors = ProviderAccessOperatorAdminValidator.ValidatePasswordChange(
            command.Email,
            command.CurrentPassword,
            command.NewPassword);

        if (errors.Count > 0)
        {
            return Result<ProviderAccessOperatorResponse>.Failure(errors);
        }

        var result = await _client.ChangeOperatorPasswordAsync(
            new ChangeProviderOperatorPasswordRequest(
                command.Email.Trim(),
                command.CurrentPassword,
                command.NewPassword),
            cancellationToken);

        return result.IsSuccess
            ? Result<ProviderAccessOperatorResponse>.Success(result.Operator!)
            : Result<ProviderAccessOperatorResponse>.Failure(
                ProviderAccessOperatorAdminValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail,
                    credentialTarget: "currentPassword"));
    }
}
