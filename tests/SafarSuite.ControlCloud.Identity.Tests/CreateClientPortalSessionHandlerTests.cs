using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.ClientPortal;

namespace SafarSuite.ControlCloud.Identity.Tests;

public sealed class CreateClientPortalSessionHandlerTests
{
    [Fact]
    public async Task EnrolledUserRequiresMfaAfterPasswordVerification()
    {
        var harness = CreateHarness();

        var result = await harness.Handler.HandleAsync(new CreateClientPortalSessionCommand(
            harness.User.ClientId,
            harness.User.Email,
            Password));

        Assert.False(result.IsSuccess);
        Assert.Equal("ClientPortalMfaRequired", result.FailureCode);
        Assert.Equal(0, harness.Sessions.CreatedSessionCount);
    }

    [Fact]
    public async Task TotpCodeIssuesOneSessionAndTheAcceptedStepCannotBeReplayed()
    {
        var harness = CreateHarness();

        var first = await harness.Handler.HandleAsync(new CreateClientPortalSessionCommand(
            harness.User.ClientId,
            harness.User.Email,
            Password,
            TotpCode: ValidTotpCode));
        var replay = await harness.Handler.HandleAsync(new CreateClientPortalSessionCommand(
            harness.User.ClientId,
            harness.User.Email,
            Password,
            TotpCode: ValidTotpCode));

        Assert.True(first.IsSuccess);
        Assert.Equal(AcceptedTotpStep, harness.User.LastTotpStep);
        Assert.False(replay.IsSuccess);
        Assert.Equal("ClientPortalMfaInvalid", replay.FailureCode);
        Assert.Equal(1, harness.Sessions.CreatedSessionCount);
    }

    [Fact]
    public async Task RecoveryCodeIssuesOneSessionAndIsConsumed()
    {
        var harness = CreateHarness();
        var normalizedCode = harness.Credentials.NormalizeRecoveryCode(RecoveryCode);
        harness.User.RecoveryCodeHashes =
        [
            harness.Credentials.HashSecret($"client-portal-recovery:{normalizedCode}")
        ];

        var first = await harness.Handler.HandleAsync(new CreateClientPortalSessionCommand(
            harness.User.ClientId,
            harness.User.Email,
            Password,
            RecoveryCode: RecoveryCode));
        var replay = await harness.Handler.HandleAsync(new CreateClientPortalSessionCommand(
            harness.User.ClientId,
            harness.User.Email,
            Password,
            RecoveryCode: RecoveryCode));

        Assert.True(first.IsSuccess);
        Assert.Empty(harness.User.RecoveryCodeHashes);
        Assert.Equal(harness.Clock.UtcNow, harness.User.LastRecoveryCodeUsedAtUtc);
        Assert.False(replay.IsSuccess);
        Assert.Equal("ClientPortalMfaInvalid", replay.FailureCode);
        Assert.Equal(1, harness.Sessions.CreatedSessionCount);
    }

    private const string Password = "PortalPassword123!";
    private const string ValidTotpCode = "123456";
    private const string RecoveryCode = "ABCD-EFGH-IJKL";
    private const long AcceptedTotpStep = 42;

    private static TestHarness CreateHarness()
    {
        var clock = new FixedClock(
            new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));
        var credentials = new HmacClientPortalCredentialService(new ClientPortalAccessOptions
        {
            SessionSigningSecret = "login-handler-unit-test-secret"
        });
        var user = ControlCloudClientPortalUser.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "owner@example.test",
            "Portal Owner",
            "ClientOwner",
            credentials.HashPassword(Password),
            clock.UtcNow.AddDays(-1));
        user.ProtectedTotpSecret = "protected-secret";
        user.TotpEnabledAtUtc = clock.UtcNow.AddHours(-1);

        var identities = new FakeIdentityRepository(user);
        var sessions = new FakeSessionService(clock);
        var handler = new CreateClientPortalSessionHandler(
            identities,
            credentials,
            sessions,
            new FakeTotpService(),
            new FakeMfaSecretProtector(),
            new NullAuditRecorder(),
            new ImmediateUnitOfWork(),
            clock);

        return new TestHarness(handler, user, credentials, sessions, clock);
    }

    private sealed record TestHarness(
        CreateClientPortalSessionHandler Handler,
        ControlCloudClientPortalUser User,
        HmacClientPortalCredentialService Credentials,
        FakeSessionService Sessions,
        FixedClock Clock);

    private sealed class FixedClock : IControlCloudClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class ImmediateUnitOfWork : IControlCloudUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default) =>
            await operation(cancellationToken);

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default) =>
            await operation(cancellationToken);
    }

    private sealed class FakeIdentityRepository : IClientPortalIdentityRepository
    {
        private readonly ControlCloudClientPortalUser _user;

        public FakeIdentityRepository(ControlCloudClientPortalUser user)
        {
            _user = user;
        }

        public Task<ControlCloudClientPortalUser?> GetUserByClientAndEmailAsync(
            Guid clientId,
            string email,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ControlCloudClientPortalUser?>(
                _user.ClientId == clientId
                && _user.Email.Equals(email, StringComparison.OrdinalIgnoreCase)
                    ? _user
                    : null);

        public Task<ControlCloudClientPortalUser?> GetUserByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ControlCloudClientPortalUser?>(
                _user.UserId == userId ? _user : null);

        public Task SaveUserAsync(
            ControlCloudClientPortalUser user,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ControlCloudClientPortalInvitation?> GetInvitationByIdAsync(
            Guid invitationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ControlCloudClientPortalInvitation?>(null);

        public Task<ControlCloudClientPortalInvitation?> GetInvitationByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ControlCloudClientPortalInvitation?>(null);

        public Task<IReadOnlyCollection<ControlCloudClientPortalInvitation>> ListInvitationsByClientIdAsync(
            Guid clientId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ControlCloudClientPortalInvitation>>([]);

        public Task AddInvitationAsync(
            ControlCloudClientPortalInvitation invitation,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SaveInvitationAsync(
            ControlCloudClientPortalInvitation invitation,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddUserAsync(
            ControlCloudClientPortalUser user,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeSessionService : IClientPortalSessionService
    {
        private readonly FixedClock _clock;

        public FakeSessionService(FixedClock clock)
        {
            _clock = clock;
        }

        public int CreatedSessionCount { get; private set; }

        public Task<CreateClientPortalSessionResult> CreateSessionAsync(
            Guid clientId,
            string role,
            CancellationToken cancellationToken = default) =>
            CreateSessionAsync(Guid.Empty, clientId, role, 1, cancellationToken);

        public Task<CreateClientPortalSessionResult> CreateSessionAsync(
            Guid userId,
            Guid clientId,
            string role,
            int securityVersion,
            CancellationToken cancellationToken = default)
        {
            CreatedSessionCount++;
            return Task.FromResult(CreateClientPortalSessionResult.Success(
                userId,
                clientId,
                "access-token",
                "refresh-token",
                _clock.UtcNow.AddMinutes(5),
                _clock.UtcNow.AddMinutes(30),
                role));
        }

        public ClientPortalSessionValidationResult Validate(string? authorizationHeader) =>
            ClientPortalSessionValidationResult.Failure("NotUsed", "Not used in this test.");
    }

    private sealed class FakeTotpService : IClientPortalTotpService
    {
        public string CreateSecret() => "secret";

        public string CreateOtpAuthUri(string issuer, string accountName, string secret) => "otpauth://test";

        public string CreateQrCodeSvg(string value) => "<svg />";

        public string CreateQrCodeDataUri(string value) => "data:image/svg+xml;base64,";

        public bool TryVerifyCode(
            string secret,
            string? code,
            DateTimeOffset now,
            long? lastAcceptedStep,
            out long acceptedStep)
        {
            acceptedStep = AcceptedTotpStep;
            return code == ValidTotpCode
                && (lastAcceptedStep is null || lastAcceptedStep < AcceptedTotpStep);
        }
    }

    private sealed class FakeMfaSecretProtector : IClientPortalMfaSecretProtector
    {
        public string Protect(string secret) => $"protected:{secret}";

        public bool TryUnprotect(string protectedSecret, out string secret)
        {
            secret = "secret";
            return protectedSecret == "protected-secret";
        }
    }

    private sealed class NullAuditRecorder : IClientPortalAuditRecorder
    {
        public Task RecordAsync(
            ClientPortalAuditRecord audit,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
