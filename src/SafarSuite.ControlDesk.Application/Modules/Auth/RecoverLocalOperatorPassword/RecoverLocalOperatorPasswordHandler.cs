using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Application.Modules.Auth.RecoverLocalOperatorPassword;

public sealed class RecoverLocalOperatorPasswordHandler(
    ILocalOperatorRepository operators,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILocalOperatorPasswordCodec passwords)
{
    public async Task<Result<RecoverLocalOperatorPasswordResult>> HandleAsync(
        RecoverLocalOperatorPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationError = Validate(command);

        if (validationError is not null)
        {
            return Result<RecoverLocalOperatorPasswordResult>.Failure(validationError);
        }

        var actor = command.Actor.Trim();
        var reason = command.Reason.Trim();
        var targetIdentity = command.TargetOperatorIdOrEmail.Trim();

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var targetResult = await ResolveTargetAsync(targetIdentity, transactionCancellationToken);

            if (targetResult.Error is not null)
            {
                return Result<RecoverLocalOperatorPasswordResult>.Failure(targetResult.Error);
            }

            var target = targetResult.LocalOperator!;
            var recoveredAtUtc = clock.UtcNow;
            target.ChangePasswordHash(passwords.Hash(command.NewPassword), recoveredAtUtc);
            await unitOfWork.SaveChangesAsync(transactionCancellationToken);

            return Result<RecoverLocalOperatorPasswordResult>.Success(
                new RecoverLocalOperatorPasswordResult(
                    target.Id.Value,
                    target.Email,
                    target.SecurityVersion,
                    recoveredAtUtc,
                    actor,
                    reason));
        }, cancellationToken);
    }

    private async Task<TargetResolution> ResolveTargetAsync(
        string targetIdentity,
        CancellationToken cancellationToken)
    {
        if (Guid.TryParse(targetIdentity, out var operatorId) && operatorId != Guid.Empty)
        {
            var localOperator = await operators.GetByIdAsync(
                LocalOperatorId.Create(operatorId),
                cancellationToken);

            return localOperator is null
                ? TargetResolution.NotFound()
                : TargetResolution.Found(localOperator);
        }

        LocalOperatorEmail email;

        try
        {
            email = LocalOperatorEmail.Create(targetIdentity);
        }
        catch (ArgumentException exception)
        {
            return TargetResolution.Failed(ApplicationError.Validation(
                nameof(RecoverLocalOperatorPasswordCommand.TargetOperatorIdOrEmail),
                exception.Message));
        }

        var matches = await operators.ListByNormalizedEmailAsync(
            email.NormalizedValue,
            cancellationToken);

        return matches.Count switch
        {
            0 => TargetResolution.NotFound(),
            1 => TargetResolution.Found(matches.Single()),
            _ => TargetResolution.Failed(ApplicationError.Conflict(
                nameof(RecoverLocalOperatorPasswordCommand.TargetOperatorIdOrEmail),
                "The recovery identity matches more than one local operator."))
        };
    }

    private static ApplicationError? Validate(RecoverLocalOperatorPasswordCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.TargetOperatorIdOrEmail))
        {
            return ApplicationError.Validation(
                nameof(command.TargetOperatorIdOrEmail),
                "Target operator id or email is required.");
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            return ApplicationError.Validation(
                nameof(command.NewPassword),
                "New password is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Actor) || command.Actor.Trim().Length > 200)
        {
            return ApplicationError.Validation(
                nameof(command.Actor),
                "Recovery actor is required and cannot exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > 1_000)
        {
            return ApplicationError.Validation(
                nameof(command.Reason),
                "Recovery reason is required and cannot exceed 1000 characters.");
        }

        return null;
    }

    private sealed record TargetResolution(
        LocalOperator? LocalOperator,
        ApplicationError? Error)
    {
        public static TargetResolution Found(LocalOperator localOperator) =>
            new(localOperator, null);

        public static TargetResolution NotFound() =>
            Failed(ApplicationError.NotFound(
                nameof(RecoverLocalOperatorPasswordCommand.TargetOperatorIdOrEmail),
                "The local operator was not found."));

        public static TargetResolution Failed(ApplicationError error) => new(null, error);
    }
}
