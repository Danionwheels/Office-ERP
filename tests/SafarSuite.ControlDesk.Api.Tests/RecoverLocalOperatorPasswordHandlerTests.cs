using SafarSuite.ControlDesk.Application.Modules.Auth;
using SafarSuite.ControlDesk.Application.Modules.Auth.RecoverLocalOperatorPassword;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class RecoverLocalOperatorPasswordHandlerTests
{
    private static readonly Guid OperatorId =
        Guid.Parse("083930bf-923f-4516-a990-aa93277a1dda");

    [Fact]
    public async Task Id_recovery_replaces_hash_and_invalidates_existing_sessions()
    {
        var target = LocalOperatorManagementTestContext.SupportOperator(
            OperatorId,
            "support@example.test");
        target.Disable(new DateTimeOffset(2026, 7, 20, 9, 30, 0, TimeSpan.Zero));
        var context = new LocalOperatorManagementTestContext(target);
        var passwords = new PasswordCodecDouble();
        var handler = CreateHandler(context, passwords);

        var result = await handler.HandleAsync(new RecoverLocalOperatorPasswordCommand(
            OperatorId.ToString(),
            "replacement-password",
            "  OFFICE\\RecoveryAdmin  ",
            "  Operator forgot the password.  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(OperatorId, result.Value.OperatorId);
        Assert.Equal("support@example.test", result.Value.Email);
        Assert.Equal(3, result.Value.SecurityVersion);
        Assert.Equal(context.Now, result.Value.RecoveredAtUtc);
        Assert.Equal("OFFICE\\RecoveryAdmin", result.Value.Actor);
        Assert.Equal("Operator forgot the password.", result.Value.Reason);
        Assert.Equal("hash:replacement-password", target.PasswordHash);
        Assert.Equal(1, passwords.HashCount);
        Assert.Equal(1, context.UnitOfWork.TransactionCount);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
        Assert.DoesNotContain(
            result.Value.GetType().GetProperties(),
            property => property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Hash", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Email_recovery_uses_normalized_identity()
    {
        var target = LocalOperatorManagementTestContext.SupportOperator(
            OperatorId,
            "support@example.test");
        var context = new LocalOperatorManagementTestContext(target);
        var handler = CreateHandler(context, new PasswordCodecDouble());

        var result = await handler.HandleAsync(new RecoverLocalOperatorPasswordCommand(
            "  SUPPORT@example.test ",
            "replacement-password",
            "Recovery Administrator",
            "Authorized offline recovery"));

        Assert.True(result.IsSuccess);
        Assert.Equal(OperatorId, result.Value.OperatorId);
        Assert.Equal(2, target.SecurityVersion);
    }

    [Fact]
    public async Task Missing_target_returns_not_found_without_hashing_or_saving()
    {
        var context = new LocalOperatorManagementTestContext();
        var passwords = new PasswordCodecDouble();
        var handler = CreateHandler(context, passwords);

        var result = await handler.HandleAsync(new RecoverLocalOperatorPasswordCommand(
            "missing@example.test",
            "replacement-password",
            "Recovery Administrator",
            "Authorized offline recovery"));

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", Assert.Single(result.Errors).Code);
        Assert.Equal(0, passwords.HashCount);
        Assert.Equal(0, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Ambiguous_email_returns_conflict_without_hashing_or_saving()
    {
        var first = LocalOperatorManagementTestContext.SupportOperator(
            OperatorId,
            "duplicate@example.test");
        var second = LocalOperatorManagementTestContext.SupportOperator(
            Guid.Parse("1ff3d024-6937-4466-85c8-ad3292b57e80"),
            "DUPLICATE@example.test");
        var context = new LocalOperatorManagementTestContext(first, second);
        var passwords = new PasswordCodecDouble();
        var handler = CreateHandler(context, passwords);

        var result = await handler.HandleAsync(new RecoverLocalOperatorPasswordCommand(
            "duplicate@example.test",
            "replacement-password",
            "Recovery Administrator",
            "Authorized offline recovery"));

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", Assert.Single(result.Errors).Code);
        Assert.Equal(0, passwords.HashCount);
        Assert.Equal(0, context.UnitOfWork.SaveCount);
    }

    [Theory]
    [InlineData("", "replacement-password", "Recovery Administrator", "Reason")]
    [InlineData("support@example.test", "", "Recovery Administrator", "Reason")]
    [InlineData("support@example.test", "replacement-password", "", "Reason")]
    [InlineData("support@example.test", "replacement-password", "Recovery Administrator", "")]
    public async Task Required_recovery_evidence_is_validated_before_a_transaction(
        string target,
        string password,
        string actor,
        string reason)
    {
        var context = new LocalOperatorManagementTestContext();
        var passwords = new PasswordCodecDouble();
        var handler = CreateHandler(context, passwords);

        var result = await handler.HandleAsync(new RecoverLocalOperatorPasswordCommand(
            target,
            password,
            actor,
            reason));

        Assert.True(result.IsFailure);
        Assert.Equal("validation", Assert.Single(result.Errors).Code);
        Assert.Equal(0, context.UnitOfWork.TransactionCount);
        Assert.Equal(0, passwords.HashCount);
    }

    private static RecoverLocalOperatorPasswordHandler CreateHandler(
        LocalOperatorManagementTestContext context,
        PasswordCodecDouble passwords) =>
        new(context.Operators, context.UnitOfWork, context.Clock, passwords);

    private sealed class PasswordCodecDouble : ILocalOperatorPasswordCodec
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
