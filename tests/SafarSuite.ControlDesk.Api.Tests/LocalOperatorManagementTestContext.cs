using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Modules.Auth;
using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Api.Tests;

internal sealed class LocalOperatorManagementTestContext(params LocalOperator[] localOperators)
{
    public DateTimeOffset Now { get; } =
        new(2026, 7, 20, 10, 0, 0, TimeSpan.Zero);

    public LocalOperatorRepositoryDouble Operators { get; } = new(localOperators);

    public UnitOfWorkDouble UnitOfWork { get; } = new();

    public LocalOperatorAdministratorGuard AdministratorGuard => new(Operators);

    public IClock Clock => new FixedClock(Now);

    public static LocalOperator Administrator(Guid id, string email) =>
        LocalOperator.CreateFirstAdministrator(
            LocalOperatorId.Create(id),
            LocalOperatorEmail.Create(email),
            "Administrator",
            "stored-hash",
            new DateTimeOffset(2026, 7, 20, 9, 0, 0, TimeSpan.Zero));

    public static LocalOperator SupportOperator(Guid id, string email) =>
        LocalOperator.Create(
            LocalOperatorId.Create(id),
            LocalOperatorEmail.Create(email),
            "Support Operator",
            "stored-hash",
            [LocalOperatorRole.SupportOperator],
            [LocalOperatorScope.DiagnosticsRead],
            new DateTimeOffset(2026, 7, 20, 9, 0, 0, TimeSpan.Zero));

    internal sealed class LocalOperatorRepositoryDouble(params LocalOperator[] localOperators)
        : ILocalOperatorRepository
    {
        private readonly List<LocalOperator> _operators = [.. localOperators];

        public int AdministratorLockCount { get; private set; }

        public Task AddAsync(
            LocalOperator localOperator,
            CancellationToken cancellationToken = default)
        {
            _operators.Add(localOperator);
            return Task.CompletedTask;
        }

        public Task<LocalOperator?> GetByIdAsync(
            LocalOperatorId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_operators.SingleOrDefault(candidate => candidate.Id == id));

        public Task<LocalOperator?> GetByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_operators.SingleOrDefault(candidate =>
                candidate.NormalizedEmail == normalizedEmail));

        public Task<bool> ExistsByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_operators.Any(candidate =>
                candidate.NormalizedEmail == normalizedEmail));

        public Task AcquireAdministratorMutationLockAsync(
            CancellationToken cancellationToken = default)
        {
            AdministratorLockCount++;
            return Task.CompletedTask;
        }

        public Task<bool> HasOtherActiveAdministratorAsync(
            LocalOperatorId excludedOperatorId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_operators.Any(candidate =>
                candidate.Id != excludedOperatorId
                && candidate.Status == LocalOperatorStatus.Active
                && candidate.Roles.Contains(LocalOperatorRole.Administrator, StringComparer.Ordinal)
                && candidate.Scopes.Contains(LocalOperatorScope.Admin, StringComparer.Ordinal)));
    }

    internal sealed class UnitOfWorkDouble : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public int TransactionCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            TransactionCount++;
            await operation(cancellationToken);
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            TransactionCount++;
            return await operation(cancellationToken);
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;

        public DateOnly Today => DateOnly.FromDateTime(now.UtcDateTime);
    }
}
