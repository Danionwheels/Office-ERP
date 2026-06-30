using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.GetClientAccountingProfile;

public sealed class GetClientAccountingProfileHandler
{
    private readonly IClientRepository _clients;
    private readonly IClientAccountingProfileRepository _profiles;

    public GetClientAccountingProfileHandler(
        IClientRepository clients,
        IClientAccountingProfileRepository profiles)
    {
        _clients = clients;
        _profiles = profiles;
    }

    public async Task<Result<ClientAccountingProfileResult>> HandleAsync(
        GetClientAccountingProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = ClientId.Create(query.ClientId);

            if (await _clients.GetByIdAsync(clientId, cancellationToken) is null)
            {
                return Result<ClientAccountingProfileResult>.Failure(ApplicationError.NotFound(
                    nameof(query.ClientId),
                    "Client was not found."));
            }

            var profile = await _profiles.GetByClientIdAsync(clientId, cancellationToken);

            if (profile is null)
            {
                return Result<ClientAccountingProfileResult>.Failure(ApplicationError.NotFound(
                    nameof(query.ClientId),
                    "Client accounting profile was not found."));
            }

            return Result<ClientAccountingProfileResult>.Success(new ClientAccountingProfileResult(
                profile.ClientId.Value,
                profile.AccountsReceivableAccountId.Value,
                profile.DefaultCurrencyCode,
                profile.CloudCustomerId,
                profile.CreatedAtUtc,
                profile.UpdatedAtUtc));
        }
        catch (ArgumentException exception)
        {
            return Result<ClientAccountingProfileResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(query),
                exception.Message));
        }
    }
}
