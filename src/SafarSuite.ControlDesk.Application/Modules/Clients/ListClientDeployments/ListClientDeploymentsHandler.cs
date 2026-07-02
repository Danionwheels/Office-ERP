using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.ListClientDeployments;

public sealed class ListClientDeploymentsHandler
{
    private readonly IClientRepository _clients;
    private readonly IClientDeploymentRepository _deployments;

    public ListClientDeploymentsHandler(
        IClientRepository clients,
        IClientDeploymentRepository deployments)
    {
        _clients = clients;
        _deployments = deployments;
    }

    public async Task<Result<ListClientDeploymentsResult>> HandleAsync(
        ListClientDeploymentsQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = ClientId.Create(query.ClientId);

            if (await _clients.GetByIdAsync(clientId, cancellationToken) is null)
            {
                return Result<ListClientDeploymentsResult>.Failure(ApplicationError.NotFound(
                    nameof(query.ClientId),
                    "Client was not found."));
            }

            var deployments = await _deployments.ListByClientIdAsync(clientId, cancellationToken);

            return Result<ListClientDeploymentsResult>.Success(new ListClientDeploymentsResult(
                query.ClientId,
                deployments.Select(ClientDeploymentResultMapper.ToResult).ToArray()));
        }
        catch (ArgumentException exception)
        {
            return Result<ListClientDeploymentsResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(query),
                exception.Message));
        }
    }
}
