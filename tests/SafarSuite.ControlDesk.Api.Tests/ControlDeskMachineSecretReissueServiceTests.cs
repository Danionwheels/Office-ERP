using SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class ControlDeskMachineSecretReissueServiceTests
{
    [Fact]
    public void Reissue_returns_metadata_only_and_changes_generation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"control-desk-reissue-{Guid.NewGuid():N}.json");
        var store = new ControlDeskMachineSecretEnvelopeStore(
            path,
            new TestMachineSecretProtector(),
            TimeProvider.System,
            $"test-reissue-{Guid.NewGuid():N}",
            TimeSpan.FromSeconds(5),
            null);
        using var first = store.CreateOrLoad();
        var service = new ControlDeskMachineSecretReissueService(store);

        try
        {
            var result = service.Reissue("offline-recovery", "Owner-approved recovery drill");

            Assert.NotEqual(first.GenerationId, result.GenerationId);
            Assert.StartsWith("control-desk-session-", result.SessionSigningKeyId, StringComparison.Ordinal);
            Assert.Equal("offline-recovery", result.Actor);
            Assert.DoesNotContain("Owner-approved", result.CiphertextFingerprint, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Reissue_requires_actor_and_reason()
    {
        var store = new ControlDeskMachineSecretEnvelopeStore(
            Path.Combine(Path.GetTempPath(), $"control-desk-reissue-{Guid.NewGuid():N}.json"),
            new TestMachineSecretProtector(),
            TimeProvider.System,
            $"test-reissue-{Guid.NewGuid():N}",
            TimeSpan.FromSeconds(5),
            null);
        var service = new ControlDeskMachineSecretReissueService(store);

        Assert.Throws<ArgumentException>(() => service.Reissue(" ", "reason"));
        Assert.Throws<ArgumentException>(() => service.Reissue("actor", " "));
    }

    private sealed class TestMachineSecretProtector : IMachineSecretProtector
    {
        public byte[] Protect(byte[] plaintext) => plaintext.ToArray();

        public byte[] Unprotect(byte[] ciphertext) => ciphertext.ToArray();
    }
}
