using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Application.Modules.Auth.DisableLocalOperator;

public sealed class DisableLocalOperatorHandler(
    ILocalOperatorRepository operators,
    IUnitOfWork unitOfWork,
    IClock clock,
    LocalOperatorAdministratorGuard administratorGuard)
{
    public async Task<Result<DisableLocalOperatorResult>> HandleAsync(
        DisableLocalOperatorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.TargetOperatorId == Guid.Empty)
        {
            return Result<DisableLocalOperatorResult>.Failure(ApplicationError.Validation(
                nameof(command.TargetOperatorId),
                "Target operator id is required."));
        }

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            await operators.AcquireAdministratorMutationLockAsync(transactionCancellationToken);

            if (!await administratorGuard.IsAuthorizedAsync(
                    command.ActingOperatorId,
                    transactionCancellationToken))
            {
                return Result<DisableLocalOperatorResult>.Failure(ApplicationError.Forbidden(
                    nameof(command.ActingOperatorId),
                    "An active local Administrator is required."));
            }

            var targetId = LocalOperatorId.Create(command.TargetOperatorId);
            var target = await operators.GetByIdAsync(targetId, transactionCancellationToken);

            if (target is null)
            {
                return Result<DisableLocalOperatorResult>.Failure(ApplicationError.NotFound(
                    nameof(command.TargetOperatorId),
                    "The local operator was not found."));
            }

            if (target.Status == LocalOperatorStatus.Disabled)
            {
                return Result<DisableLocalOperatorResult>.Success(ToResult(target));
            }

            if (IsAdministrator(target)
                && !await operators.HasOtherActiveAdministratorAsync(
                    targetId,
                    transactionCancellationToken))
            {
                return Result<DisableLocalOperatorResult>.Failure(ApplicationError.Conflict(
                    nameof(command.TargetOperatorId),
                    "The last active local Administrator cannot be disabled."));
            }

            target.Disable(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(transactionCancellationToken);

            return Result<DisableLocalOperatorResult>.Success(ToResult(target));
        }, cancellationToken);
    }

    private static bool IsAdministrator(LocalOperator localOperator) =>
        localOperator.Roles.Contains(LocalOperatorRole.Administrator, StringComparer.Ordinal)
        && localOperator.Scopes.Contains(LocalOperatorScope.Admin, StringComparer.Ordinal);

    private static DisableLocalOperatorResult ToResult(LocalOperator localOperator) => new(
        localOperator.Id.Value,
        localOperator.Status.ToString(),
        localOperator.SecurityVersion,
        localOperator.UpdatedAtUtc);
}
