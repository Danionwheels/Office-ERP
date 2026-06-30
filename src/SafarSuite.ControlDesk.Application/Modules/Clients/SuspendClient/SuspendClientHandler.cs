using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.SuspendClient;

public sealed class SuspendClientHandler
{
    private readonly IClientRepository _clients;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SuspendClientHandler(
        IClientRepository clients,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _clients = clients;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ClientDetailsResult>> HandleAsync(
        SuspendClientCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _clients.GetByIdAsync(
                ClientId.Create(command.ClientId),
                cancellationToken);

            if (client is null)
            {
                return Result<ClientDetailsResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClientId),
                    "Client was not found."));
            }

            client.Suspend(_clock.UtcNow);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ClientDetailsResult>.Success(ClientResultMapper.ToDetailsResult(client));
        }
        catch (ArgumentException exception)
        {
            return Result<ClientDetailsResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }
}
