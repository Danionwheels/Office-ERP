using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Domain.Modules.Auth;
using SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class EfLocalOperatorRepositoryPostgresTests
{
    private const string ConnectionStringEnvironmentVariable =
        "SAFARSUITE_OPERATOR_REPOSITORY_TEST_CONNECTION_STRING";

    private const string RequiredEnvironmentVariable =
        "SAFARSUITE_REQUIRE_OPERATOR_REPOSITORY_POSTGRES_TEST";

    [Fact]
    public async Task Repository_round_trips_aggregate_and_enforces_identity_and_admin_lock_contracts()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);
        var isRequired = string.Equals(
            Environment.GetEnvironmentVariable(RequiredEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.False(
                isRequired,
                $"{ConnectionStringEnvironmentVariable} is required by the PostgreSQL repository gate.");
            return;
        }

        var firstId = LocalOperatorId.Create(Guid.NewGuid());
        var secondId = LocalOperatorId.Create(Guid.NewGuid());
        var duplicateId = LocalOperatorId.Create(Guid.NewGuid());
        var uniqueSuffix = firstId.Value.ToString("N");
        var firstEmail = $"operator-{uniqueSuffix}@example.test";
        var normalizedEmail = firstEmail.ToUpperInvariant();

        try
        {
            await using (var context = CreateContext(connectionString))
            {
                await context.Database.MigrateAsync();
                var repository = new EfLocalOperatorRepository(context);

                await repository.AddAsync(LocalOperator.CreateFirstAdministrator(
                    firstId,
                    LocalOperatorEmail.Create(firstEmail),
                    "First Administrator",
                    "hash:first",
                    new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero)));
                await repository.AddAsync(LocalOperator.CreateFirstAdministrator(
                    secondId,
                    LocalOperatorEmail.Create($"second-{uniqueSuffix}@example.test"),
                    "Second Administrator",
                    "hash:second",
                    new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero)));
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();

                var loaded = await repository.GetByNormalizedEmailAsync(normalizedEmail);

                Assert.NotNull(loaded);
                Assert.Equal(firstId, loaded!.Id);
                Assert.Equal(LocalOperatorRole.Administrator, Assert.Single(loaded.Roles));
                Assert.Equal(LocalOperatorScope.Admin, Assert.Single(loaded.Scopes));
                Assert.True(await repository.ExistsByNormalizedEmailAsync(normalizedEmail));
                Assert.Single(await repository.ListByNormalizedEmailAsync(normalizedEmail));

                await using var transaction = await context.Database.BeginTransactionAsync();
                await repository.AcquireAdministratorMutationLockAsync();
                Assert.True(await repository.HasOtherActiveAdministratorAsync(firstId));
                await transaction.CommitAsync();
            }

            await using (var duplicateContext = CreateContext(connectionString))
            {
                var duplicateRepository = new EfLocalOperatorRepository(duplicateContext);
                await duplicateRepository.AddAsync(LocalOperator.Create(
                    duplicateId,
                    LocalOperatorEmail.Create(firstEmail.ToUpperInvariant()),
                    "Duplicate Identity",
                    "hash:duplicate",
                    [LocalOperatorRole.SupportOperator],
                    [LocalOperatorScope.DiagnosticsRead],
                    new DateTimeOffset(2026, 7, 20, 12, 1, 0, TimeSpan.Zero)));

                await Assert.ThrowsAsync<DbUpdateException>(() =>
                    duplicateContext.SaveChangesAsync());
            }
        }
        finally
        {
            await using var cleanupContext = CreateContext(connectionString);
            await cleanupContext.LocalOperators
                .Where(localOperator =>
                    localOperator.Id == firstId
                    || localOperator.Id == secondId
                    || localOperator.Id == duplicateId)
                .ExecuteDeleteAsync();
        }
    }

    private static ControlDeskDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ControlDeskDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "control"))
            .Options;

        return new ControlDeskDbContext(options);
    }
}
