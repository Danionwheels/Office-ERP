using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.UpdateClient;

public sealed class UpdateClientHandler
{
    private readonly IClientRepository _clients;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateClientValidator _validator;

    public UpdateClientHandler(
        IClientRepository clients,
        IUnitOfWork unitOfWork,
        UpdateClientValidator validator)
    {
        _clients = clients;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<ClientDetailsResult>> HandleAsync(
        UpdateClientCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<ClientDetailsResult>.Failure(validationErrors);
        }

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

            client.Rename(command.LegalName, command.DisplayName);

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
