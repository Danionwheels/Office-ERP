using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.AddClientSupportNote;

public sealed class AddClientSupportNoteHandler
{
    private readonly IClientRepository _clients;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly AddClientSupportNoteValidator _validator;

    public AddClientSupportNoteHandler(
        IClientRepository clients,
        IUnitOfWork unitOfWork,
        IClock clock,
        AddClientSupportNoteValidator validator)
    {
        _clients = clients;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<ClientSupportNoteResult>> HandleAsync(
        AddClientSupportNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _validator.Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<ClientSupportNoteResult>.Failure(validationErrors);
        }

        try
        {
            var client = await _clients.GetByIdAsync(
                ClientId.Create(command.ClientId),
                cancellationToken);

            if (client is null)
            {
                return Result<ClientSupportNoteResult>.Failure(ApplicationError.NotFound(
                    nameof(command.ClientId),
                    "Client was not found."));
            }

            var note = SupportNote.Create(command.Text, command.CreatedBy, _clock.UtcNow);

            client.AddSupportNote(note);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ClientSupportNoteResult>.Success(ClientResultMapper.ToSupportNoteResult(note));
        }
        catch (ArgumentException exception)
        {
            return Result<ClientSupportNoteResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }
}
