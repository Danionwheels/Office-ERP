using SafarSuite.ControlDesk.Application.Modules.Auth.ChangeLocalOperatorAccess;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class ChangeLocalOperatorAccessHandlerTests
{
    private static readonly Guid AdministratorId =
        Guid.Parse("42fb270a-7165-4ca5-a62c-e6bcedd542a8");

    private static readonly Guid SecondAdministratorId =
        Guid.Parse("d6135560-5405-4513-b55a-5ccaacc83cd3");

    private static readonly Guid SupportOperatorId =
        Guid.Parse("03614052-32d2-480f-aa57-7a2b654cbe69");

    [Fact]
    public async Task Active_administrator_changes_access_and_invalidates_target_sessions()
    {
        var actor = LocalOperatorManagementTestContext.Administrator(
            AdministratorId,
            "admin@example.test");
        var target = LocalOperatorManagementTestContext.SupportOperator(
            SupportOperatorId,
            "support@example.test");
        var context = new LocalOperatorManagementTestContext(actor, target);
        var handler = CreateHandler(context);

        var result = await handler.HandleAsync(new ChangeLocalOperatorAccessCommand(
            AdministratorId,
            SupportOperatorId,
            ["auditor"],
            ["REPORTS:READ"]));

        Assert.True(result.IsSuccess);
        Assert.Equal(LocalOperatorRole.Auditor, Assert.Single(result.Value.Roles));
        Assert.Equal(LocalOperatorScope.ReportsRead, Assert.Single(result.Value.Scopes));
        Assert.Equal(2, result.Value.SecurityVersion);
        Assert.Equal(context.Now, result.Value.UpdatedAtUtc);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
        Assert.Equal(1, context.Operators.AdministratorLockCount);
    }

    [Fact]
    public async Task Last_active_administrator_cannot_be_demoted()
    {
        var target = LocalOperatorManagementTestContext.Administrator(
            AdministratorId,
            "only-admin@example.test");
        var context = new LocalOperatorManagementTestContext(target);
        var handler = CreateHandler(context);

        var result = await handler.HandleAsync(new ChangeLocalOperatorAccessCommand(
            AdministratorId,
            AdministratorId,
            [LocalOperatorRole.Auditor],
            [LocalOperatorScope.ReportsRead]));

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", Assert.Single(result.Errors).Code);
        Assert.Equal(LocalOperatorRole.Administrator, Assert.Single(target.Roles));
        Assert.Equal(1, target.SecurityVersion);
        Assert.Equal(0, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Administrator_can_be_demoted_when_another_active_administrator_exists()
    {
        var target = LocalOperatorManagementTestContext.Administrator(
            AdministratorId,
            "first-admin@example.test");
        var actor = LocalOperatorManagementTestContext.Administrator(
            SecondAdministratorId,
            "second-admin@example.test");
        var context = new LocalOperatorManagementTestContext(target, actor);
        var handler = CreateHandler(context);

        var result = await handler.HandleAsync(new ChangeLocalOperatorAccessCommand(
            SecondAdministratorId,
            AdministratorId,
            [LocalOperatorRole.Auditor],
            [LocalOperatorScope.ReportsRead]));

        Assert.True(result.IsSuccess);
        Assert.Equal(LocalOperatorRole.Auditor, Assert.Single(target.Roles));
        Assert.Equal(2, target.SecurityVersion);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Canonically_equivalent_access_is_idempotent()
    {
        var actor = LocalOperatorManagementTestContext.Administrator(
            AdministratorId,
            "admin@example.test");
        var target = LocalOperatorManagementTestContext.SupportOperator(
            SupportOperatorId,
            "support@example.test");
        var context = new LocalOperatorManagementTestContext(actor, target);
        var handler = CreateHandler(context);

        var result = await handler.HandleAsync(new ChangeLocalOperatorAccessCommand(
            AdministratorId,
            SupportOperatorId,
            [" supportoperator ", "SUPPORTOPERATOR"],
            [" diagnostics:read "]));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.SecurityVersion);
        Assert.Equal(0, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Missing_target_returns_not_found_without_saving()
    {
        var actor = LocalOperatorManagementTestContext.Administrator(
            AdministratorId,
            "admin@example.test");
        var context = new LocalOperatorManagementTestContext(actor);
        var handler = CreateHandler(context);

        var result = await handler.HandleAsync(new ChangeLocalOperatorAccessCommand(
            AdministratorId,
            SupportOperatorId,
            [LocalOperatorRole.Auditor],
            [LocalOperatorScope.ReportsRead]));

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", Assert.Single(result.Errors).Code);
        Assert.Equal(0, context.UnitOfWork.SaveCount);
    }

    private static ChangeLocalOperatorAccessHandler CreateHandler(
        LocalOperatorManagementTestContext context) =>
        new(
            context.Operators,
            context.UnitOfWork,
            context.Clock,
            context.AdministratorGuard);
}
