using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Identity.Tests;

public sealed class ClientPortalUserRecoveryCodeTests
{
    [Fact]
    public void RecoveryCodeCanBeConsumedOnlyOnce()
    {
        var now = new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);
        var user = ControlCloudClientPortalUser.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "owner@example.test",
            "Portal Owner",
            "ClientOwner",
            "password-hash",
            now);
        user.RecoveryCodeHashes = ["hash-one", "hash-two"];

        var firstUse = user.ConsumeRecoveryCode("hash-one", now.AddMinutes(1));
        var secondUse = user.ConsumeRecoveryCode("hash-one", now.AddMinutes(2));

        Assert.True(firstUse);
        Assert.False(secondUse);
        Assert.Equal(["hash-two"], user.RecoveryCodeHashes);
        Assert.Equal(now.AddMinutes(1), user.LastRecoveryCodeUsedAtUtc);
    }
}
