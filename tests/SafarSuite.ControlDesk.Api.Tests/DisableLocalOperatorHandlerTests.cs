using SafarSuite.ControlDesk.Application.Modules.Auth.DisableLocalOperator;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class DisableLocalOperatorHandlerTests
{
    private static readonly Guid FirstAdministratorId =
        Guid.Parse("6003decc-cd85-4479-8b9a-08b7d9fa59de");

    private static readonly Guid SecondAdministratorId =
        Guid.Parse("4625d3fe-e747-41ce-b0e1-fd5ecee0066d");

    private static readonly Guid SupportOperatorId =
        Guid.Parse("18550c86-32de-4d07-b802-f0c026b3fac3");

    [Fact]
    public async Task Another_active_administrator_can_disable_target_and_invalidate_sessions()
    {
        var target = LocalOperatorManagementTestContext.Administrator(
            FirstAdministratorId,
            "first-admin@example.test");
        var actor = LocalOperatorManagementTestContext.Administrator(
            SecondAdministratorId,
            "second-admin@example.test");
        var context = new LocalOperatorManagementTestContext(target, actor);
        var handler = CreateHandler(context);

        var result = await handler.HandleAsync(new DisableLocalOperatorCommand(
            SecondAdministratorId,
            FirstAdministratorId));

        Assert.True(result.IsSuccess);
        Assert.Equal(LocalOperatorStatus.Disabled.ToString(), result.Value.Status);
        Assert.Equal(2, result.Value.SecurityVersion);
        Assert.Equal(context.Now, result.Value.UpdatedAtUtc);
        Assert.Equal(1, context.UnitOfWork.TransactionCount);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
        Assert.Equal(1, context.Operators.AdministratorLockCount);
    }

    [Fact]
    public async Task Last_active_administrator_cannot_be_disabled()
    {
        var target = LocalOperatorManagementTestContext.Administrator(
            FirstAdministratorId,
            "only-admin@example.test");
        var context = new LocalOperatorManagementTestContext(target);
        var handler = CreateHandler(context);

        var result = await handler.HandleAsync(new DisableLocalOperatorCommand(
            FirstAdministratorId,
            FirstAdministratorId));

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", Assert.Single(result.Errors).Code);
        Assert.Equal(LocalOperatorStatus.Active, target.Status);
        Assert.Equal(1, target.SecurityVersion);
        Assert.Equal(0, context.UnitOfWork.SaveCount);
        Assert.Equal(1, context.Operators.AdministratorLockCount);
    }

    [Fact]
    public async Task Non_administrator_is_forbidden_before_target_lookup()
    {
        var actor = LocalOperatorManagementTestContext.SupportOperator(
            SupportOperatorId,
            "support@example.test");
        var context = new LocalOperatorManagementTestContext(actor);
        var handler = CreateHandler(context);

        var result = await handler.HandleAsync(new DisableLocalOperatorCommand(
            SupportOperatorId,
            Guid.Parse("2f873d86-b99b-43f2-a5a5-b351efad4162")));

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(0, context.UnitOfWork.SaveCount);
        Assert.Equal(1, context.Operators.AdministratorLockCount);
    }

    [Fact]
    public async Task Missing_target_returns_not_found_without_saving()
    {
        var actor = LocalOperatorManagementTestContext.Administrator(
            FirstAdministratorId,
            "admin@example.test");
        var context = new LocalOperatorManagementTestContext(actor);
        var handler = CreateHandler(context);

        var result = await handler.HandleAsync(new DisableLocalOperatorCommand(
            FirstAdministratorId,
            Guid.Parse("2f873d86-b99b-43f2-a5a5-b351efad4162")));

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", Assert.Single(result.Errors).Code);
        Assert.Equal(0, context.UnitOfWork.SaveCount);
    }

    private static DisableLocalOperatorHandler CreateHandler(
        LocalOperatorManagementTestContext context) =>
        new(
            context.Operators,
            context.UnitOfWork,
            context.Clock,
            context.AdministratorGuard);
}
