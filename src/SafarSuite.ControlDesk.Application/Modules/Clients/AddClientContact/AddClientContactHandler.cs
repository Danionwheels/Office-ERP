using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.AddClientContact;

public sealed class AddClientContactHandler
{
    private readonly IClientRepository _clients;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly AddClientContactValidator _validator;

    public AddClientContactHandler(
        IClientRepository clients,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IClock clock,
        AddClientContactValidator validator)
    {
        _clients = clients;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<ClientContactResult>> HandleAsync(
        AddClientContactCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<ClientContactResult>.Failure(validationErrors);
        }

        try
        {
            var client = await _clients.GetByIdAsync(
                ClientId.Create(command.ClientId),
                cancellationToken);

            if (client is null)
            {
                return Result<ClientContactResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClientId),
                    "Client was not found."));
            }

            if (!Enum.TryParse<ClientContactRole>(command.Role, ignoreCase: true, out var role)
                || !Enum.IsDefined(role))
            {
                return Result<ClientContactResult>.Failure(ApplicationError.Validation(
                    nameof(command.Role),
                    "Client contact role is invalid."));
            }

            var contact = ClientContact.Create(
                ClientContactId.Create(_idGenerator.NewGuid()),
                role,
                command.FullName,
                command.JobTitle,
                command.Email,
                command.Phone,
                command.IsPrimary,
                _clock.UtcNow);

            client.AddContact(contact);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ClientContactResult>.Success(ClientResultMapper.ToContactResult(contact));
        }
        catch (ArgumentException exception)
        {
            return Result<ClientContactResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }
}
