using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Application.Modules.Auth.CreateLocalOperator;

public sealed class CreateLocalOperatorHandler(
    ILocalOperatorRepository operators,
    IUnitOfWork unitOfWork,
    IIdGenerator idGenerator,
    IClock clock,
    ILocalOperatorPasswordCodec passwords,
    LocalOperatorAdministratorGuard administratorGuard)
{
    public async Task<Result<CreateLocalOperatorResult>> HandleAsync(
        CreateLocalOperatorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!await administratorGuard.IsAuthorizedAsync(
                command.ActingOperatorId,
                cancellationToken))
        {
            return Result<CreateLocalOperatorResult>.Failure(ApplicationError.Forbidden(
                nameof(command.ActingOperatorId),
                "An active local Administrator is required."));
        }

        try
        {
            if (string.IsNullOrWhiteSpace(command.Password))
            {
                return Result<CreateLocalOperatorResult>.Failure(ApplicationError.Validation(
                    nameof(command.Password),
                    "Password is required."));
            }

            var email = LocalOperatorEmail.Create(command.Email);

            return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
            {
                if (await operators.ExistsByNormalizedEmailAsync(
                        email.NormalizedValue,
                        transactionCancellationToken))
                {
                    return Result<CreateLocalOperatorResult>.Failure(ApplicationError.Conflict(
                        nameof(command.Email),
                        "A local operator with this email already exists."));
                }

                var localOperator = LocalOperator.Create(
                    LocalOperatorId.Create(idGenerator.NewGuid()),
                    email,
                    command.FullName,
                    passwords.Hash(command.Password),
                    command.Roles,
                    command.Scopes,
                    clock.UtcNow);

                await operators.AddAsync(localOperator, transactionCancellationToken);
                await unitOfWork.SaveChangesAsync(transactionCancellationToken);

                return Result<CreateLocalOperatorResult>.Success(new CreateLocalOperatorResult(
                    localOperator.Id.Value,
                    localOperator.Email,
                    localOperator.FullName,
                    localOperator.Status.ToString(),
                    localOperator.Roles.ToArray(),
                    localOperator.Scopes.ToArray(),
                    localOperator.SecurityVersion,
                    localOperator.CreatedAtUtc));
            }, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Result<CreateLocalOperatorResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }
}
