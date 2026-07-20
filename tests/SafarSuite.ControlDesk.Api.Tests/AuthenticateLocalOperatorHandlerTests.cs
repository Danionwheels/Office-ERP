using SafarSuite.ControlDesk.Application.Modules.Auth;
using SafarSuite.ControlDesk.Application.Modules.Auth.AuthenticateLocalOperator;
using SafarSuite.ControlDesk.Application.Modules.Auth.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Auth;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class AuthenticateLocalOperatorHandlerTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 7, 20, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Valid_credentials_return_a_password_free_session_principal()
    {
        var localOperator = CreateOperator();
        var repository = new StubRepository(localOperator);
        var passwords = new StubPasswordCodec("correct-password");
        var handler = new AuthenticateLocalOperatorHandler(repository, passwords);

        var result = await handler.HandleAsync(new AuthenticateLocalOperatorCommand(
            "  ADMIN@example.test ",
            "correct-password"));

        Assert.True(result.IsAuthenticated);
        Assert.NotNull(result.Principal);
        Assert.Equal(localOperator.Id.Value, result.Principal.OperatorId);
        Assert.Equal(localOperator.Email, result.Principal.Email);
        Assert.Equal(localOperator.SecurityVersion, result.Principal.SecurityVersion);
        Assert.Equal(localOperator.Roles, result.Principal.Roles);
        Assert.Equal(localOperator.Scopes, result.Principal.Scopes);
        Assert.Equal("ADMIN@EXAMPLE.TEST", repository.LastNormalizedEmail);
        Assert.DoesNotContain(
            result.Principal.GetType().GetProperties(),
            property => property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("unknown@example.test", "correct-password")]
    [InlineData("admin@example.test", "wrong-password")]
    [InlineData("not-an-email", "correct-password")]
    [InlineData("admin@example.test", "")]
    public async Task Invalid_credentials_return_the_same_generic_failure(
        string email,
        string password)
    {
        var handler = new AuthenticateLocalOperatorHandler(
            new StubRepository(CreateOperator()),
            new StubPasswordCodec("correct-password"));

        var result = await handler.HandleAsync(new AuthenticateLocalOperatorCommand(email, password));

        Assert.Same(AuthenticateLocalOperatorResult.Failed, result);
        Assert.False(result.IsAuthenticated);
        Assert.Null(result.Principal);
    }

    [Fact]
    public async Task Disabled_operator_returns_the_same_generic_failure()
    {
        var localOperator = CreateOperator();
        localOperator.Disable(CreatedAt.AddMinutes(1));
        var handler = new AuthenticateLocalOperatorHandler(
            new StubRepository(localOperator),
            new StubPasswordCodec("correct-password"));

        var result = await handler.HandleAsync(new AuthenticateLocalOperatorCommand(
            localOperator.Email,
            "correct-password"));

        Assert.Same(AuthenticateLocalOperatorResult.Failed, result);
    }

    private static LocalOperator CreateOperator() => LocalOperator.CreateFirstAdministrator(
        LocalOperatorId.Create(Guid.Parse("f684a364-2a37-4482-aea8-dc48f4371f29")),
        LocalOperatorEmail.Create("admin@example.test"),
        "Office Administrator",
        "stored-password-hash",
        CreatedAt);

    private sealed class StubRepository(LocalOperator localOperator) : ILocalOperatorRepository
    {
        public string? LastNormalizedEmail { get; private set; }

        public Task<LocalOperator?> GetByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            LastNormalizedEmail = normalizedEmail;
            return Task.FromResult(
                string.Equals(
                    normalizedEmail,
                    localOperator.NormalizedEmail,
                    StringComparison.Ordinal)
                    ? localOperator
                    : null);
        }
    }

    private sealed class StubPasswordCodec(string acceptedPassword) : ILocalOperatorPasswordCodec
    {
        public string Hash(string password) => $"hash:{password}";

        public bool Verify(string password, string? passwordHash) =>
            password == acceptedPassword && passwordHash == "stored-password-hash";
    }
}
