using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Domain.Modules.Auth;
using SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class EfLocalOperatorRepositoryTests
{
    [Fact]
    public async Task Administrator_mutation_lock_refuses_to_run_outside_a_transaction()
    {
        var options = new DbContextOptionsBuilder<ControlDeskDbContext>()
            .UseNpgsql("Host=localhost;Database=not_contacted;Username=test;Password=test")
            .Options;
        await using var context = new ControlDeskDbContext(options);
        var repository = new EfLocalOperatorRepository(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AcquireAdministratorMutationLockAsync());

        Assert.Contains("active database transaction", exception.Message);
    }

    [Fact]
    public void Other_active_administrator_query_uses_both_grant_tables()
    {
        var options = new DbContextOptionsBuilder<ControlDeskDbContext>()
            .UseNpgsql("Host=localhost;Database=not_contacted;Username=test;Password=test")
            .Options;
        using var context = new ControlDeskDbContext(options);
        var repository = new EfLocalOperatorRepository(context);

        var sql = repository.BuildOtherActiveAdministratorQuery(
                LocalOperatorId.Create(Guid.Parse("8b027809-39ed-4373-ab46-d2e57b1bef01")))
            .ToQueryString();

        Assert.Contains("auth.local_operators", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("auth.local_operator_roles", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("auth.local_operator_scopes", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Administrator", sql, StringComparison.Ordinal);
        Assert.Contains("control-desk:admin", sql, StringComparison.Ordinal);
        Assert.Contains("Active", sql, StringComparison.Ordinal);
    }
}
