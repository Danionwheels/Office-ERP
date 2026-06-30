using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.CreateClient;

public sealed class CreateClientHandler
{
    private readonly IClientRepository _clients;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly CreateClientValidator _validator;

    public CreateClientHandler(
        IClientRepository clients,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        CreateClientValidator validator)
    {
        _clients = clients;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<CreateClientResult>> HandleAsync(
        CreateClientCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<CreateClientResult>.Failure(validationErrors);
        }

        try
        {
            var code = ClientCode.Create(command.Code);

            if (await _clients.ExistsByCodeAsync(code, cancellationToken))
            {
                return Result<CreateClientResult>.Failure(ApplicationError.Conflict(
                    nameof(command.Code),
                    $"Client {code.Value} already exists."));
            }

            var client = Client.Create(
                ClientId.Create(_idGenerator.NewGuid()),
                code,
                command.LegalName,
                command.DisplayName,
                _clock.UtcNow);

            await _clients.AddAsync(client, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CreateClientResult>.Success(new CreateClientResult(
                client.Id.Value,
                client.Code.Value,
                client.LegalName,
                client.DisplayName,
                client.Status.ToString()));
        }
        catch (ArgumentException exception)
        {
            return Result<CreateClientResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }
}
