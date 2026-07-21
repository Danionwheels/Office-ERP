using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Modules.Auth;
using SafarSuite.ControlDesk.Application.Modules.Auth.CreateLocalOperator;
using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class CreateLocalOperatorHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 20, 9, 0, 0, TimeSpan.Zero);

    private static readonly Guid AdministratorId =
        Guid.Parse("9e40e6fb-c44a-4648-89d4-a385ab07992d");

    private static readonly Guid NewOperatorId =
        Guid.Parse("14bd266f-9211-485b-8d28-fe825325a0ce");

    [Fact]
    public async Task Active_administrator_creates_operator_transactionally_with_canonical_access()
    {
        var repository = new FakeRepository(CreateAdministrator());
        var unitOfWork = new FakeUnitOfWork();
        var passwords = new FakePasswordCodec();
        var handler = CreateHandler(repository, unitOfWork, passwords);

        var result = await handler.HandleAsync(new CreateLocalOperatorCommand(
            AdministratorId,
            "  SUPPORT@example.test ",
            " Support Operator ",
            "temporary-password",
            ["supportoperator"],
            ["DIAGNOSTICS:READ"]));

        Assert.True(result.IsSuccess);
        Assert.Equal(NewOperatorId, result.Value.OperatorId);
        Assert.Equal("SUPPORT@example.test", result.Value.Email);
        Assert.Equal("Support Operator", result.Value.FullName);
        Assert.Equal(LocalOperatorRole.SupportOperator, Assert.Single(result.Value.Roles));
        Assert.Equal(LocalOperatorScope.DiagnosticsRead, Assert.Single(result.Value.Scopes));
        Assert.Equal(1, result.Value.SecurityVersion);
        Assert.Equal(Now, result.Value.CreatedAtUtc);
        Assert.Equal("hash:temporary-password", Assert.Single(repository.Added).PasswordHash);
        Assert.Equal(1, unitOfWork.TransactionCount);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal(1, passwords.HashCount);
        Assert.DoesNotContain(
            result.Value.GetType().GetProperties(),
            property => property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Existing_normalized_email_returns_conflict_without_hashing_or_saving()
    {
        var existing = LocalOperator.Create(
            LocalOperatorId.Create(Guid.Parse("e5f8d87d-edbb-4f23-9af2-c3aeaa962b3d")),
            LocalOperatorEmail.Create("support@example.test"),
            "Existing Support",
            "existing-hash",
            [LocalOperatorRole.SupportOperator],
            [LocalOperatorScope.DiagnosticsRead],
            Now);
        var repository = new FakeRepository(CreateAdministrator(), existing);
        var unitOfWork = new FakeUnitOfWork();
        var passwords = new FakePasswordCodec();
        var handler = CreateHandler(repository, unitOfWork, passwords);

        var result = await handler.HandleAsync(ValidCommand() with
        {
            Email = " SUPPORT@example.test "
        });

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", Assert.Single(result.Errors).Code);
        Assert.Empty(repository.Added);
        Assert.Equal(1, unitOfWork.TransactionCount);
        Assert.Equal(0, unitOfWork.SaveCount);
        Assert.Equal(0, passwords.HashCount);
    }

    [Fact]
    public async Task Non_administrator_returns_forbidden_before_target_or_password_processing()
    {
        var supportActor = LocalOperator.Create(
            LocalOperatorId.Create(AdministratorId),
            LocalOperatorEmail.Create("support-actor@example.test"),
            "Support Actor",
            "stored-hash",
            [LocalOperatorRole.SupportOperator],
            [LocalOperatorScope.DiagnosticsRead],
            Now);
        var repository = new FakeRepository(supportActor);
        var unitOfWork = new FakeUnitOfWork();
        var passwords = new FakePasswordCodec();
        var handler = CreateHandler(repository, unitOfWork, passwords);

        var result = await handler.HandleAsync(ValidCommand());

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(0, unitOfWork.TransactionCount);
        Assert.Equal(0, passwords.HashCount);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task Invalid_access_combination_returns_validation_without_saving()
    {
        var repository = new FakeRepository(CreateAdministrator());
        var unitOfWork = new FakeUnitOfWork();
        var passwords = new FakePasswordCodec();
        var handler = CreateHandler(repository, unitOfWork, passwords);

        var result = await handler.HandleAsync(ValidCommand() with
        {
            Roles = [LocalOperatorRole.Administrator],
            Scopes = [LocalOperatorScope.DiagnosticsRead]
        });

        Assert.True(result.IsFailure);
        Assert.Equal("validation", Assert.Single(result.Errors).Code);
        Assert.Equal(1, unitOfWork.TransactionCount);
        Assert.Equal(0, unitOfWork.SaveCount);
        Assert.Empty(repository.Added);
    }

    private static CreateLocalOperatorCommand ValidCommand() => new(
        AdministratorId,
        "new.operator@example.test",
        "New Operator",
        "temporary-password",
        [LocalOperatorRole.SupportOperator],
        [LocalOperatorScope.DiagnosticsRead]);

    private static CreateLocalOperatorHandler CreateHandler(
        FakeRepository repository,
        FakeUnitOfWork unitOfWork,
        FakePasswordCodec passwords) =>
        new(
            repository,
            unitOfWork,
            new FixedIdGenerator(NewOperatorId),
            new FixedClock(Now),
            passwords,
            new LocalOperatorAdministratorGuard(repository));

    private static LocalOperator CreateAdministrator() =>
        LocalOperator.CreateFirstAdministrator(
            LocalOperatorId.Create(AdministratorId),
            LocalOperatorEmail.Create("admin@example.test"),
            "Administrator",
            "admin-hash",
            Now);

    private sealed class FakeRepository(params LocalOperator[] operators) : ILocalOperatorRepository
    {
        private readonly List<LocalOperator> _operators = [.. operators];

        public List<LocalOperator> Added { get; } = [];

        public Task AddAsync(
            LocalOperator localOperator,
            CancellationToken cancellationToken = default)
        {
            Added.Add(localOperator);
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

        public Task<IReadOnlyCollection<LocalOperator>> ListByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<LocalOperator>>(_operators
                .Where(candidate => candidate.NormalizedEmail == normalizedEmail)
                .ToArray());

        public Task<bool> ExistsByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_operators.Any(candidate =>
                candidate.NormalizedEmail == normalizedEmail));

        public Task AcquireAdministratorMutationLockAsync(
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> HasOtherActiveAdministratorAsync(
            LocalOperatorId excludedOperatorId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_operators.Any(candidate =>
                candidate.Id != excludedOperatorId
                && candidate.Status == LocalOperatorStatus.Active
                && candidate.Roles.Contains(LocalOperatorRole.Administrator, StringComparer.Ordinal)
                && candidate.Scopes.Contains(LocalOperatorScope.Admin, StringComparer.Ordinal)));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
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

    private sealed class FixedIdGenerator(Guid id) : IIdGenerator
    {
        public Guid NewGuid() => id;
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;

        public DateOnly Today => DateOnly.FromDateTime(now.UtcDateTime);
    }

    private sealed class FakePasswordCodec : ILocalOperatorPasswordCodec
    {
        public int HashCount { get; private set; }

        public string Hash(string password)
        {
            HashCount++;
            return $"hash:{password}";
        }

        public bool Verify(string password, string? passwordHash) =>
            throw new NotSupportedException();
    }
}
