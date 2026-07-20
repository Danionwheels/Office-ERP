using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Application.Modules.Auth.Ports;

public interface ILocalOperatorRepository
{
    Task AddAsync(LocalOperator localOperator, CancellationToken cancellationToken = default);

    Task<LocalOperator?> GetByIdAsync(
        LocalOperatorId id,
        CancellationToken cancellationToken = default);

    Task<LocalOperator?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task AcquireAdministratorMutationLockAsync(
        CancellationToken cancellationToken = default);

    Task<bool> HasOtherActiveAdministratorAsync(
        LocalOperatorId excludedOperatorId,
        CancellationToken cancellationToken = default);
}
