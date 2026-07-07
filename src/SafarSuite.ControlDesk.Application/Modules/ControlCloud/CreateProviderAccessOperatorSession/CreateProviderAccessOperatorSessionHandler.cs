using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateProviderAccessOperatorSession;

public sealed class CreateProviderAccessOperatorSessionHandler
{
    private readonly IControlCloudProviderAccessClient _client;

    public CreateProviderAccessOperatorSessionHandler(IControlCloudProviderAccessClient client)
    {
        _client = client;
    }

    public async Task<Result<ProviderAccessSessionResponse>> HandleAsync(
        CreateProviderAccessOperatorSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        var errors = ProviderAccessOperatorAdminValidator.ValidateSession(
            command.Email,
            command.Password,
            command.Scopes,
            command.ExpiresInMinutes);

        if (errors.Count > 0)
        {
            return Result<ProviderAccessSessionResponse>.Failure(errors);
        }

        var result = await _client.CreateOperatorSessionAsync(
            new CreateProviderOperatorSessionRequest(
                command.Email.Trim(),
                command.Password,
                ProviderAccessOperatorAdminValidator.NormalizeOptionalScopes(command.Scopes),
                command.ExpiresInMinutes,
                command.RecoveryCode,
                command.TotpCode),
            cancellationToken);

        return result.IsSuccess
            ? Result<ProviderAccessSessionResponse>.Success(result.Session!)
            : Result<ProviderAccessSessionResponse>.Failure(
                ProviderAccessOperatorAdminValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }
}
