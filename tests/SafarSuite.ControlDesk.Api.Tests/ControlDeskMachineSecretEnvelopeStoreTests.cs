using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class ControlDeskMachineSecretEnvelopeStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"safarsuite-machine-secrets-{Guid.NewGuid():N}");
    private readonly AuthenticatedTestProtector _protector = new();

    public ControlDeskMachineSecretEnvelopeStoreTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void CreateOrLoad_writes_versioned_envelope_and_preserves_generation()
    {
        var store = CreateStore();

        using var created = store.CreateOrLoad();
        using var loaded = store.CreateOrLoad();
        var createdKey = created.CopySessionSigningKey();
        var loadedKey = loaded.CopySessionSigningKey();

        try
        {
            Assert.Equal(created.GenerationId, loaded.GenerationId);
            Assert.Equal(created.CreatedAtUtc, loaded.CreatedAtUtc);
            Assert.Equal(created.SessionSigningKeyId, loaded.SessionSigningKeyId);
            Assert.Equal(created.CiphertextFingerprint, loaded.CiphertextFingerprint);
            Assert.True(CryptographicOperations.FixedTimeEquals(createdKey, loadedKey));
            Assert.Equal(32, createdKey.Length);

            using var document = JsonDocument.Parse(File.ReadAllBytes(EnvelopePath));
            var root = document.RootElement;
            Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
            Assert.Equal("WindowsDpapiLocalMachine", root.GetProperty("protectionKind").GetString());
            Assert.Equal("SafarSuiteControlDesk", root.GetProperty("product").GetString());
            Assert.Equal("ControlDeskSessionSigning", root.GetProperty("purpose").GetString());
            Assert.Equal(created.CiphertextFingerprint, root.GetProperty("ciphertextSha256").GetString());
            Assert.False(root.TryGetProperty("sessionSigningKey", out _));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(createdKey);
            CryptographicOperations.ZeroMemory(loadedKey);
        }
    }

    [Fact]
    public void Replace_creates_new_generation_and_key()
    {
        var store = CreateStore();
        using var previous = store.CreateOrLoad();
        var previousKey = previous.CopySessionSigningKey();

        using var replacement = store.Replace();
        var replacementKey = replacement.CopySessionSigningKey();

        try
        {
            Assert.NotEqual(previous.GenerationId, replacement.GenerationId);
            Assert.NotEqual(previous.SessionSigningKeyId, replacement.SessionSigningKeyId);
            Assert.NotEqual(previous.CiphertextFingerprint, replacement.CiphertextFingerprint);
            Assert.False(CryptographicOperations.FixedTimeEquals(previousKey, replacementKey));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(previousKey);
            CryptographicOperations.ZeroMemory(replacementKey);
        }
    }

    [Fact]
    public void Interruption_after_temporary_flush_preserves_previous_valid_envelope() =>
        AssertInterruptedReplacementPreservesPreviousEnvelope(
            MachineSecretWriteStage.TemporaryFileFlushed);

    [Fact]
    public void Interruption_after_atomic_replacement_rolls_back_previous_valid_envelope() =>
        AssertInterruptedReplacementPreservesPreviousEnvelope(
            MachineSecretWriteStage.EnvelopeReplaced);

    private void AssertInterruptedReplacementPreservesPreviousEnvelope(
        MachineSecretWriteStage interruptionStage)
    {
        using var original = CreateStore().CreateOrLoad();
        var originalBytes = File.ReadAllBytes(EnvelopePath);
        var interruptedStore = CreateStore(stage =>
        {
            if (stage == interruptionStage)
            {
                throw new SimulatedInterruptionException();
            }
        });

        Assert.Throws<SimulatedInterruptionException>(() => interruptedStore.Replace());
        Assert.Equal(originalBytes, File.ReadAllBytes(EnvelopePath));

        using var recovered = CreateStore().Read();
        Assert.Equal(original.GenerationId, recovered.GenerationId);
        Assert.Empty(Directory.GetFiles(_directory, ".*.tmp"));
        Assert.Empty(Directory.GetFiles(_directory, ".*.bak"));
    }

    [Fact]
    public void Read_rejects_ciphertext_fingerprint_tampering()
    {
        using var created = CreateStore().CreateOrLoad();
        var envelope = JsonNode.Parse(File.ReadAllText(EnvelopePath))!.AsObject();
        envelope["ciphertextSha256"] = new string('0', 64);
        File.WriteAllText(EnvelopePath, envelope.ToJsonString());

        var exception = Assert.Throws<MachineSecretEnvelopeException>(() => CreateStore().Read());

        Assert.Equal(MachineSecretEnvelopeFailure.InvalidEnvelope, exception.Failure);
    }

    [Fact]
    public void Read_rejects_modified_ciphertext_even_with_recomputed_fingerprint()
    {
        using var created = CreateStore().CreateOrLoad();
        var envelope = JsonNode.Parse(File.ReadAllText(EnvelopePath))!.AsObject();
        var ciphertext = Convert.FromBase64String(envelope["ciphertext"]!.GetValue<string>());
        ciphertext[^1] ^= 0x01;
        envelope["ciphertext"] = Convert.ToBase64String(ciphertext);
        envelope["ciphertextSha256"] = Convert.ToHexStringLower(SHA256.HashData(ciphertext));
        File.WriteAllText(EnvelopePath, envelope.ToJsonString());

        var exception = Assert.Throws<MachineSecretEnvelopeException>(() => CreateStore().Read());

        Assert.Equal(MachineSecretEnvelopeFailure.InvalidEnvelope, exception.Failure);
    }

    [Fact]
    public async Task Concurrent_create_or_load_converges_on_one_generation()
    {
        var snapshots = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            CreateStore().CreateOrLoad())));

        try
        {
            Assert.Single(snapshots.Select(snapshot => snapshot.GenerationId).Distinct());
            Assert.Single(snapshots.Select(snapshot => snapshot.CiphertextFingerprint).Distinct());
        }
        finally
        {
            foreach (var snapshot in snapshots)
            {
                snapshot.Dispose();
            }
        }
    }

    [Fact]
    public void Snapshot_text_contains_evidence_but_not_key_material()
    {
        using var snapshot = CreateStore().CreateOrLoad();
        var key = snapshot.CopySessionSigningKey();

        try
        {
            var text = snapshot.ToString();
            Assert.Contains(snapshot.GenerationId.ToString("D"), text, StringComparison.Ordinal);
            Assert.Contains(snapshot.CiphertextFingerprint, text, StringComparison.Ordinal);
            Assert.DoesNotContain(Convert.ToBase64String(key), text, StringComparison.Ordinal);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    [Fact]
    public void Windows_dpapi_local_machine_protector_round_trips()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var protector = new WindowsDpapiMachineSecretProtector();
        var plaintext = Encoding.UTF8.GetBytes($"machine-secret-proof-{Guid.NewGuid():N}");
        var ciphertext = protector.Protect(plaintext);
        var recovered = protector.Unprotect(ciphertext);

        try
        {
            Assert.NotEqual(plaintext, ciphertext);
            Assert.Equal(plaintext, recovered);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(ciphertext);
            CryptographicOperations.ZeroMemory(recovered);
        }
    }

    public void Dispose()
    {
        _protector.Dispose();

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private string EnvelopePath => Path.Combine(_directory, "control-desk-machine-secrets.v1.json");

    private ControlDeskMachineSecretEnvelopeStore CreateStore(
        Action<MachineSecretWriteStage>? observer = null) =>
        new(
            EnvelopePath,
            _protector,
            TimeProvider.System,
            $"SafarSuiteMachineSecretTests-{GetType().Name}-{Path.GetFileName(_directory)}",
            TimeSpan.FromSeconds(10),
            observer);

    private sealed class SimulatedInterruptionException : Exception;

    private sealed class AuthenticatedTestProtector : IMachineSecretProtector, IDisposable
    {
        private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);

        public byte[] Protect(byte[] plaintext)
        {
            var nonce = RandomNumberGenerator.GetBytes(12);
            var tag = new byte[16];
            var encrypted = new byte[plaintext.Length];

            using (var aes = new AesGcm(_key, tag.Length))
            {
                aes.Encrypt(nonce, plaintext, encrypted, tag);
            }

            var result = new byte[nonce.Length + tag.Length + encrypted.Length];
            nonce.CopyTo(result, 0);
            tag.CopyTo(result, nonce.Length);
            encrypted.CopyTo(result, nonce.Length + tag.Length);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(tag);
            CryptographicOperations.ZeroMemory(encrypted);
            return result;
        }

        public byte[] Unprotect(byte[] ciphertext)
        {
            if (ciphertext.Length < 28)
            {
                throw new CryptographicException("Invalid authenticated ciphertext.");
            }

            var plaintext = new byte[ciphertext.Length - 28];

            using var aes = new AesGcm(_key, 16);
            aes.Decrypt(
                ciphertext.AsSpan(0, 12),
                ciphertext.AsSpan(28),
                ciphertext.AsSpan(12, 16),
                plaintext);
            return plaintext;
        }

        public void Dispose() => CryptographicOperations.ZeroMemory(_key);
    }
}
