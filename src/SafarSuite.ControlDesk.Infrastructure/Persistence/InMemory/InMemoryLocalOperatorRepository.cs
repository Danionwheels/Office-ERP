using System.Collections.Concurrent;
using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.InMemory;

public sealed class InMemoryLocalOperatorRepository(IEnumerable<LocalOperator> localOperators)
    : ILocalOperatorRepository
{
    private readonly ConcurrentDictionary<Guid, LocalOperator> _operators = new(
        localOperators.ToDictionary(localOperator => localOperator.Id.Value));

    public Task AddAsync(
        LocalOperator localOperator,
        CancellationToken cancellationToken = default)
    {
        _operators.TryAdd(localOperator.Id.Value, localOperator);
        return Task.CompletedTask;
    }

    public Task<LocalOperator?> GetByIdAsync(
        LocalOperatorId id,
        CancellationToken cancellationToken = default)
    {
        _operators.TryGetValue(id.Value, out var localOperator);
        return Task.FromResult(localOperator);
    }

    public Task<LocalOperator?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        var matches = FindByNormalizedEmail(normalizedEmail);
        return Task.FromResult(matches.Length == 1 ? matches[0] : null);
    }

    public Task<IReadOnlyCollection<LocalOperator>> ListByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<LocalOperator>>(
            FindByNormalizedEmail(normalizedEmail));

    public Task<bool> ExistsByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(FindByNormalizedEmail(normalizedEmail).Length > 0);

    public Task AcquireAdministratorMutationLockAsync(
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "Administrator mutations require the PostgreSQL operator repository.");

    public Task<bool> HasOtherActiveAdministratorAsync(
        LocalOperatorId excludedOperatorId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_operators.Values.Any(localOperator =>
            localOperator.Id != excludedOperatorId
            && localOperator.Status == LocalOperatorStatus.Active
            && localOperator.Roles.Contains(LocalOperatorRole.Administrator, StringComparer.Ordinal)
            && localOperator.Scopes.Contains(LocalOperatorScope.Admin, StringComparer.Ordinal)));

    private LocalOperator[] FindByNormalizedEmail(string normalizedEmail) =>
        _operators.Values
            .Where(localOperator => string.Equals(
                localOperator.NormalizedEmail,
                normalizedEmail,
                StringComparison.Ordinal))
            .ToArray();
}
