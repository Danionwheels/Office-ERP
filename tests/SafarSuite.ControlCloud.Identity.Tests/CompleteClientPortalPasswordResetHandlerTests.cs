using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.ClientPortal;

namespace SafarSuite.ControlCloud.Identity.Tests;

public sealed class CompleteClientPortalPasswordResetHandlerTests
{
    [Fact]
    public async Task ValidResetChangesPasswordConsumesTokenAndRevokesEverySession()
    {
        const string resetToken = "valid-password-reset-token";
        const string oldPassword = "OldPortalPassword123!";
        const string newPassword = "NewPortalPassword456!";
        var clock = new IdentityTestClock(
            new DateTimeOffset(2026, 7, 14, 9, 0, 0, TimeSpan.Zero));
        var credentials = new HmacClientPortalCredentialService(new ClientPortalAccessOptions
        {
            SessionSigningSecret = "password-reset-handler-test-secret"
        });
        var user = ControlCloudClientPortalUser.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "owner@example.test",
            "Portal Owner",
            "ClientOwner",
            credentials.HashPassword(oldPassword),
            clock.UtcNow.AddDays(-30));
        var reset = ControlCloudClientPortalPasswordReset.Create(
            Guid.NewGuid(),
            user.UserId,
            user.ClientId,
            credentials.HashSecret($"client-portal-password-reset:{resetToken}"),
            clock.UtcNow.AddMinutes(-5),
            clock.UtcNow.AddMinutes(25));
        var resets = new PasswordResetTestRepository(reset);
        var identities = new IdentityTestRepository(user);
        var sessions = new IdentityTestSessionService(clock);
        var handler = new CompleteClientPortalPasswordResetHandler(
            resets,
            identities,
            credentials,
            sessions,
            new IdentityTestUnitOfWork(),
            clock);

        var result = await handler.HandleAsync(
            new CompleteClientPortalPasswordResetCommand(resetToken, newPassword));

        Assert.True(result.IsSuccess);
        Assert.False(credentials.VerifyPassword(oldPassword, user.PasswordHash));
        Assert.True(credentials.VerifyPassword(newPassword, user.PasswordHash));
        Assert.Equal(clock.UtcNow, reset.UsedAtUtc);
        Assert.Equal(1, resets.SaveCount);
        Assert.Equal(1, identities.SaveUserCount);
        Assert.Equal(1, sessions.RevokeAllCount);
        Assert.Equal(user.UserId, sessions.RevokedUserId);
        Assert.Equal("Password reset completed.", sessions.RevokeReason);
    }

    private sealed class PasswordResetTestRepository : IClientPortalPasswordResetRepository
    {
        private readonly ControlCloudClientPortalPasswordReset _reset;

        public PasswordResetTestRepository(ControlCloudClientPortalPasswordReset reset)
        {
            _reset = reset;
        }

        public int SaveCount { get; private set; }

        public Task<ControlCloudClientPortalPasswordReset?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ControlCloudClientPortalPasswordReset?>(
                _reset.TokenHash.Equals(tokenHash, StringComparison.Ordinal)
                    ? _reset
                    : null);

        public Task AddAsync(
            ControlCloudClientPortalPasswordReset passwordReset,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SaveAsync(
            ControlCloudClientPortalPasswordReset passwordReset,
            CancellationToken cancellationToken = default)
        {
            Assert.Same(_reset, passwordReset);
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
