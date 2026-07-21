using System.Security.Cryptography;

namespace SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

public sealed class ControlDeskMachineSecretSnapshot : IDisposable
{
    private readonly object _sync = new();
    private byte[]? _sessionSigningKey;

    internal ControlDeskMachineSecretSnapshot(
        Guid generationId,
        DateTimeOffset createdAtUtc,
        string sessionSigningKeyId,
        byte[] sessionSigningKey,
        string ciphertextFingerprint)
    {
        GenerationId = generationId;
        CreatedAtUtc = createdAtUtc;
        SessionSigningKeyId = sessionSigningKeyId;
        _sessionSigningKey = sessionSigningKey.ToArray();
        CiphertextFingerprint = ciphertextFingerprint;
    }

    public Guid GenerationId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public string SessionSigningKeyId { get; }

    public string CiphertextFingerprint { get; }

    public byte[] CopySessionSigningKey()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_sessionSigningKey is null, this);
            return _sessionSigningKey.ToArray();
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_sessionSigningKey is null)
            {
                return;
            }

            CryptographicOperations.ZeroMemory(_sessionSigningKey);
            _sessionSigningKey = null;
        }
    }

    public override string ToString() =>
        $"Generation={GenerationId:D}; KeyId={SessionSigningKeyId}; Fingerprint={CiphertextFingerprint}";
}
