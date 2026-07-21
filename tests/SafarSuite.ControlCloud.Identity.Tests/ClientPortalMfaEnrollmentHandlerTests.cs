using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.ClientPortal;

namespace SafarSuite.ControlCloud.Identity.Tests;

public sealed class ClientPortalMfaEnrollmentHandlerTests
{
    [Fact]
    public async Task BeginEnrollmentRejectsAnIncorrectCurrentPassword()
    {
        var harness = CreateHarness();

        var result = await harness.Begin.HandleAsync(
            new BeginClientPortalMfaEnrollmentCommand(
                harness.User.UserId,
                "wrong-password"));

        Assert.False(result.IsSuccess);
        Assert.Equal("ClientPortalReauthenticationRequired", result.FailureCode);
        Assert.Null(harness.User.PendingProtectedTotpSecret);
        Assert.Equal(0, harness.Identities.SaveUserCount);
    }

    [Fact]
    public async Task ConfirmPromotesPendingEnrollmentRevokesOldSessionsAndReturnsReplacement()
    {
        var harness = CreateHarness();

        var begun = await harness.Begin.HandleAsync(
            new BeginClientPortalMfaEnrollmentCommand(
                harness.User.UserId,
                CurrentPassword));

        Assert.True(begun.IsSuccess);
        Assert.Equal(TotpSecret, begun.Secret);
        Assert.Equal(10, begun.RecoveryCodes.Count);
        Assert.Equal($"protected:{TotpSecret}", harness.User.PendingProtectedTotpSecret);
        Assert.False(harness.User.IsTotpEnabled);

        var confirmed = await harness.Confirm.HandleAsync(
            new ConfirmClientPortalMfaEnrollmentCommand(
                harness.User.UserId,
                ValidTotpCode));

        Assert.True(confirmed.IsSuccess);
        Assert.NotNull(confirmed.Session);
        Assert.Equal("replacement-access-token", confirmed.Session!.AccessToken);
        Assert.True(harness.User.IsTotpEnabled);
        Assert.Equal($"protected:{TotpSecret}", harness.User.ProtectedTotpSecret);
        Assert.Null(harness.User.PendingProtectedTotpSecret);
        Assert.Equal(AcceptedStep, harness.User.LastTotpStep);
        Assert.Equal(2, harness.User.SecurityVersion);
        Assert.Equal(10, harness.User.RecoveryCodeHashes.Length);
        Assert.Equal(2, harness.Identities.SaveUserCount);
        Assert.Equal(1, harness.Sessions.RevokeAllCount);
        Assert.Equal(harness.User.UserId, harness.Sessions.RevokedUserId);
        Assert.Equal("MFA enrollment changed.", harness.Sessions.RevokeReason);
        Assert.Equal(1, harness.Sessions.CreateSessionCount);
        Assert.Equal(["RevokeAll", "CreateSession"], harness.Sessions.Operations);
    }

    private const string CurrentPassword = "CurrentPortalPassword123!";
    private const string TotpSecret = "JBSWY3DPEHPK3PXP";
    private const string ValidTotpCode = "654321";
    private const long AcceptedStep = 1_234_567;

    private static EnrollmentHarness CreateHarness()
    {
        var clock = new IdentityTestClock(
            new DateTimeOffset(2026, 7, 14, 9, 0, 0, TimeSpan.Zero));
        var credentials = new HmacClientPortalCredentialService(new ClientPortalAccessOptions
        {
            SessionSigningSecret = "mfa-enrollment-handler-test-secret"
        });
        var user = ControlCloudClientPortalUser.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "owner@example.test",
            "Portal Owner",
            "ClientOwner",
            credentials.HashPassword(CurrentPassword),
            clock.UtcNow.AddDays(-30));
        var identities = new IdentityTestRepository(user);
        var sessions = new IdentityTestSessionService(clock);
        var totp = new EnrollmentTestTotpService();
        var secrets = new EnrollmentTestSecretProtector();
        var unitOfWork = new IdentityTestUnitOfWork();
        var begin = new BeginClientPortalMfaEnrollmentHandler(
            identities,
            credentials,
            totp,
            secrets,
            unitOfWork,
            clock);
        var confirm = new ConfirmClientPortalMfaEnrollmentHandler(
            identities,
            totp,
            secrets,
            sessions,
            unitOfWork,
            clock);

        return new EnrollmentHarness(
            begin,
            confirm,
            user,
            identities,
            sessions);
    }

    private sealed record EnrollmentHarness(
        BeginClientPortalMfaEnrollmentHandler Begin,
        ConfirmClientPortalMfaEnrollmentHandler Confirm,
        ControlCloudClientPortalUser User,
        IdentityTestRepository Identities,
        IdentityTestSessionService Sessions);

    private sealed class EnrollmentTestTotpService : IClientPortalTotpService
    {
        public string CreateSecret() => TotpSecret;

        public string CreateOtpAuthUri(
            string issuer,
            string accountName,
            string secret) =>
            $"otpauth://totp/{accountName}?secret={secret}";

        public string CreateQrCodeSvg(string value) => "<svg />";

        public string CreateQrCodeDataUri(string value) =>
            "data:image/svg+xml;base64,PHN2ZyAvPg==";

        public bool TryVerifyCode(
            string secret,
            string? code,
            DateTimeOffset now,
            long? lastAcceptedStep,
            out long acceptedStep)
        {
            acceptedStep = AcceptedStep;
            return secret == TotpSecret
                && code == ValidTotpCode
                && lastAcceptedStep is null;
        }
    }

    private sealed class EnrollmentTestSecretProtector : IClientPortalMfaSecretProtector
    {
        public string Protect(string secret) => $"protected:{secret}";

        public bool TryUnprotect(string protectedSecret, out string secret)
        {
            const string prefix = "protected:";
            var isProtected = protectedSecret.StartsWith(prefix, StringComparison.Ordinal);
            secret = isProtected ? protectedSecret[prefix.Length..] : "";
            return isProtected;
        }
    }
}
