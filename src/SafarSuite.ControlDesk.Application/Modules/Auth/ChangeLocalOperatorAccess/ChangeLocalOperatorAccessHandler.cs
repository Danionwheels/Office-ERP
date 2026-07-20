using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Application.Modules.Auth.ChangeLocalOperatorAccess;

public sealed class ChangeLocalOperatorAccessHandler(
    ILocalOperatorRepository operators,
    IUnitOfWork unitOfWork,
    IClock clock,
    LocalOperatorAdministratorGuard administratorGuard)
{
    public async Task<Result<ChangeLocalOperatorAccessResult>> HandleAsync(
        ChangeLocalOperatorAccessCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.TargetOperatorId == Guid.Empty)
        {
            return Result<ChangeLocalOperatorAccessResult>.Failure(ApplicationError.Validation(
                nameof(command.TargetOperatorId),
                "Target operator id is required."));
        }

        string[] roles;
        string[] scopes;

        try
        {
            roles = Normalize(command.Roles, LocalOperatorRole.Normalize, nameof(command.Roles));
            scopes = Normalize(command.Scopes, LocalOperatorScope.Normalize, nameof(command.Scopes));

            if (roles.Contains(LocalOperatorRole.Administrator, StringComparer.Ordinal)
                != scopes.Contains(LocalOperatorScope.Admin, StringComparer.Ordinal))
            {
                return Result<ChangeLocalOperatorAccessResult>.Failure(ApplicationError.Validation(
                    nameof(command.Roles),
                    "The Administrator role and control-desk:admin scope must be granted or removed together."));
            }
        }
        catch (ArgumentException exception)
        {
            return Result<ChangeLocalOperatorAccessResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            await operators.AcquireAdministratorMutationLockAsync(transactionCancellationToken);

            if (!await administratorGuard.IsAuthorizedAsync(
                    command.ActingOperatorId,
                    transactionCancellationToken))
            {
                return Result<ChangeLocalOperatorAccessResult>.Failure(ApplicationError.Forbidden(
                    nameof(command.ActingOperatorId),
                    "An active local Administrator is required."));
            }

            var targetId = LocalOperatorId.Create(command.TargetOperatorId);
            var target = await operators.GetByIdAsync(targetId, transactionCancellationToken);

            if (target is null)
            {
                return Result<ChangeLocalOperatorAccessResult>.Failure(ApplicationError.NotFound(
                    nameof(command.TargetOperatorId),
                    "The local operator was not found."));
            }

            var removesActiveAdministrator = target.Status == LocalOperatorStatus.Active
                && IsAdministrator(target)
                && !roles.Contains(LocalOperatorRole.Administrator, StringComparer.Ordinal);

            if (removesActiveAdministrator
                && !await operators.HasOtherActiveAdministratorAsync(
                    targetId,
                    transactionCancellationToken))
            {
                return Result<ChangeLocalOperatorAccessResult>.Failure(ApplicationError.Conflict(
                    nameof(command.TargetOperatorId),
                    "The last active local Administrator cannot lose Administrator access."));
            }

            var previousSecurityVersion = target.SecurityVersion;
            target.ChangeAccess(roles, scopes, clock.UtcNow);

            if (target.SecurityVersion != previousSecurityVersion)
            {
                await unitOfWork.SaveChangesAsync(transactionCancellationToken);
            }

            return Result<ChangeLocalOperatorAccessResult>.Success(ToResult(target));
        }, cancellationToken);
    }

    private static string[] Normalize(
        IReadOnlyCollection<string> values,
        Func<string, string> normalize,
        string parameterName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var normalized = values
            .Select(normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return normalized;
    }

    private static bool IsAdministrator(LocalOperator localOperator) =>
        localOperator.Roles.Contains(LocalOperatorRole.Administrator, StringComparer.Ordinal)
        && localOperator.Scopes.Contains(LocalOperatorScope.Admin, StringComparer.Ordinal);

    private static ChangeLocalOperatorAccessResult ToResult(LocalOperator localOperator) => new(
        localOperator.Id.Value,
        localOperator.Roles.ToArray(),
        localOperator.Scopes.ToArray(),
        localOperator.SecurityVersion,
        localOperator.UpdatedAtUtc);
}
