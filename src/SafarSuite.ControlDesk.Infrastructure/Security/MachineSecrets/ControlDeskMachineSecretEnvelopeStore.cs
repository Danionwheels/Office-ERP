using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

public sealed class ControlDeskMachineSecretEnvelopeStore
{
    public const string DefaultLifecycleMutexName = @"Global\SafarSuiteControlDeskMachineSecrets";

    private const int EnvelopeSchemaVersion = 1;
    private const int PayloadSchemaVersion = 1;
    private const int SessionSigningKeyBytes = 32;
    private const int MaximumEnvelopeBytes = 1_048_576;
    private const string ProtectionKind = "WindowsDpapiLocalMachine";
    private const string Product = "SafarSuiteControlDesk";
    private const string Purpose = "ControlDeskSessionSigning";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true
    };

    private readonly string _envelopePath;
    private readonly string _lifecycleMutexName;
    private readonly IMachineSecretProtector _protector;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _mutexTimeout;
    private readonly Action<MachineSecretWriteStage>? _writeObserver;

    [SupportedOSPlatform("windows")]
    public ControlDeskMachineSecretEnvelopeStore(string envelopePath)
        : this(
            envelopePath,
            new WindowsDpapiMachineSecretProtector(),
            TimeProvider.System,
            DefaultLifecycleMutexName,
            TimeSpan.FromSeconds(30),
            null)
    {
    }

    internal ControlDeskMachineSecretEnvelopeStore(
        string envelopePath,
        IMachineSecretProtector protector,
        TimeProvider timeProvider,
        string lifecycleMutexName,
        TimeSpan mutexTimeout,
        Action<MachineSecretWriteStage>? writeObserver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(envelopePath);
        ArgumentNullException.ThrowIfNull(protector);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(lifecycleMutexName);

        if (mutexTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(mutexTimeout));
        }

        _envelopePath = Path.GetFullPath(envelopePath);
        _protector = protector;
        _timeProvider = timeProvider;
        _lifecycleMutexName = lifecycleMutexName;
        _mutexTimeout = mutexTimeout;
        _writeObserver = writeObserver;
    }

    public ControlDeskMachineSecretSnapshot CreateOrLoad() =>
        ExecuteLocked(() => File.Exists(_envelopePath) ? ReadCore() : CreateAndWriteCore());

    public ControlDeskMachineSecretSnapshot Read() => ExecuteLocked(ReadCore);

    public ControlDeskMachineSecretSnapshot Replace() => ExecuteLocked(CreateAndWriteCore);

    private ControlDeskMachineSecretSnapshot ExecuteLocked(
        Func<ControlDeskMachineSecretSnapshot> operation)
    {
        using var mutex = new Mutex(initiallyOwned: false, _lifecycleMutexName);
        var acquired = false;

        try
        {
            try
            {
                acquired = mutex.WaitOne(_mutexTimeout);
            }
            catch (AbandonedMutexException)
            {
                acquired = true;
            }

            if (!acquired)
            {
                throw new MachineSecretEnvelopeException(
                    MachineSecretEnvelopeFailure.LifecycleBusy,
                    "The Control Desk machine-secret lifecycle is already active.");
            }

            return operation();
        }
        finally
        {
            if (acquired)
            {
                mutex.ReleaseMutex();
            }
        }
    }

    private ControlDeskMachineSecretSnapshot CreateAndWriteCore()
    {
        var generationId = Guid.NewGuid();
        var createdAtUtc = _timeProvider.GetUtcNow();
        var keyId = $"control-desk-session-{generationId:N}";
        var key = RandomNumberGenerator.GetBytes(SessionSigningKeyBytes);

        try
        {
            var payload = new ProtectedPayload
            {
                SchemaVersion = PayloadSchemaVersion,
                GenerationId = generationId,
                CreatedAtUtc = createdAtUtc,
                SessionSigningKeyId = keyId,
                SessionSigningKey = Convert.ToBase64String(key)
            };
            var plaintext = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
            byte[]? ciphertext = null;

            try
            {
                ciphertext = _protector.Protect(plaintext);
                var fingerprint = Fingerprint(ciphertext);
                var envelope = new OuterEnvelope
                {
                    SchemaVersion = EnvelopeSchemaVersion,
                    ProtectionKind = ProtectionKind,
                    Product = Product,
                    Purpose = Purpose,
                    Ciphertext = Convert.ToBase64String(ciphertext),
                    CiphertextSha256 = fingerprint
                };
                var serializedEnvelope = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);

                try
                {
                    WriteAtomically(serializedEnvelope, generationId, key, fingerprint);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(serializedEnvelope);
                }

                return new ControlDeskMachineSecretSnapshot(
                    generationId,
                    createdAtUtc,
                    keyId,
                    key,
                    fingerprint);
            }
            catch (MachineSecretEnvelopeException)
            {
                throw;
            }
            catch (Exception exception) when (exception is CryptographicException or JsonException)
            {
                throw new MachineSecretEnvelopeException(
                    MachineSecretEnvelopeFailure.ProtectionFailed,
                    "The Control Desk machine-secret payload could not be protected.",
                    exception);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);

                if (ciphertext is not null)
                {
                    CryptographicOperations.ZeroMemory(ciphertext);
                }
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private ControlDeskMachineSecretSnapshot ReadCore()
    {
        try
        {
            var fileInfo = new FileInfo(_envelopePath);

            if (!fileInfo.Exists || fileInfo.Length is <= 0 or > MaximumEnvelopeBytes)
            {
                throw InvalidEnvelope();
            }

            var serializedEnvelope = File.ReadAllBytes(_envelopePath);

            try
            {
                var envelope = JsonSerializer.Deserialize<OuterEnvelope>(serializedEnvelope, JsonOptions);

                if (envelope is null
                    || envelope.SchemaVersion != EnvelopeSchemaVersion
                    || envelope.ProtectionKind != ProtectionKind
                    || envelope.Product != Product
                    || envelope.Purpose != Purpose)
                {
                    throw InvalidEnvelope();
                }

                var ciphertext = DecodeCanonicalBase64(envelope.Ciphertext);

                try
                {
                    var fingerprint = Fingerprint(ciphertext);

                    if (!FixedTimeEquals(fingerprint, envelope.CiphertextSha256))
                    {
                        throw InvalidEnvelope();
                    }

                    var plaintext = _protector.Unprotect(ciphertext);

                    try
                    {
                        var payload = JsonSerializer.Deserialize<ProtectedPayload>(plaintext, JsonOptions);
                        return CreateValidatedSnapshot(payload, fingerprint);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(plaintext);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ciphertext);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(serializedEnvelope);
            }
        }
        catch (MachineSecretEnvelopeException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
                                           or UnauthorizedAccessException
                                           or JsonException
                                           or FormatException
                                           or CryptographicException)
        {
            throw new MachineSecretEnvelopeException(
                MachineSecretEnvelopeFailure.InvalidEnvelope,
                "The Control Desk machine-secret envelope is unavailable or invalid.",
                exception);
        }
    }

    private void WriteAtomically(
        byte[] serializedEnvelope,
        Guid expectedGenerationId,
        byte[] expectedKey,
        string expectedFingerprint)
    {
        var directory = Path.GetDirectoryName(_envelopePath)
            ?? throw new InvalidOperationException("The machine-secret envelope path must have a parent directory.");
        Directory.CreateDirectory(directory);
        var fileName = Path.GetFileName(_envelopePath);
        var temporaryPath = Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.tmp");
        var backupPath = Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.bak");
        var hadExistingEnvelope = File.Exists(_envelopePath);
        var replacementCommitted = false;

        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       FileOptions.WriteThrough))
            {
                stream.Write(serializedEnvelope);
                stream.Flush(flushToDisk: true);
            }

            _writeObserver?.Invoke(MachineSecretWriteStage.TemporaryFileFlushed);

            if (hadExistingEnvelope)
            {
                File.Replace(temporaryPath, _envelopePath, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, _envelopePath);
            }

            replacementCommitted = true;
            _writeObserver?.Invoke(MachineSecretWriteStage.EnvelopeReplaced);

            using var verified = ReadCore();
            var verifiedKey = verified.CopySessionSigningKey();

            try
            {
                if (verified.GenerationId != expectedGenerationId
                    || !FixedTimeEquals(verified.CiphertextFingerprint, expectedFingerprint)
                    || !CryptographicOperations.FixedTimeEquals(verifiedKey, expectedKey))
                {
                    throw new MachineSecretEnvelopeException(
                        MachineSecretEnvelopeFailure.WriteVerificationFailed,
                        "The written Control Desk machine-secret envelope did not verify.");
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(verifiedKey);
            }
        }
        catch
        {
            if (replacementCommitted)
            {
                RollBackReplacement(hadExistingEnvelope, backupPath);
            }

            throw;
        }
        finally
        {
            DeleteIfPresent(temporaryPath);
            DeleteIfPresent(backupPath);
        }
    }

    private void RollBackReplacement(bool hadExistingEnvelope, string backupPath)
    {
        try
        {
            if (hadExistingEnvelope && File.Exists(backupPath))
            {
                File.Replace(backupPath, _envelopePath, null, ignoreMetadataErrors: true);
            }
            else if (!hadExistingEnvelope)
            {
                DeleteIfPresent(_envelopePath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new MachineSecretEnvelopeException(
                MachineSecretEnvelopeFailure.WriteVerificationFailed,
                "The Control Desk machine-secret envelope replacement could not be rolled back safely.",
                exception);
        }
    }

    private static ControlDeskMachineSecretSnapshot CreateValidatedSnapshot(
        ProtectedPayload? payload,
        string fingerprint)
    {
        if (payload is null
            || payload.SchemaVersion != PayloadSchemaVersion
            || payload.GenerationId == Guid.Empty
            || payload.CreatedAtUtc == default
            || payload.CreatedAtUtc.Offset != TimeSpan.Zero
            || payload.SessionSigningKeyId != $"control-desk-session-{payload.GenerationId:N}")
        {
            throw InvalidEnvelope();
        }

        var key = DecodeCanonicalBase64(payload.SessionSigningKey);

        try
        {
            if (key.Length < SessionSigningKeyBytes)
            {
                throw InvalidEnvelope();
            }

            return new ControlDeskMachineSecretSnapshot(
                payload.GenerationId,
                payload.CreatedAtUtc,
                payload.SessionSigningKeyId,
                key,
                fingerprint);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] DecodeCanonicalBase64(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw InvalidEnvelope();
        }

        var decoded = Convert.FromBase64String(value);

        if (Convert.ToBase64String(decoded) != value)
        {
            CryptographicOperations.ZeroMemory(decoded);
            throw InvalidEnvelope();
        }

        return decoded;
    }

    private static string Fingerprint(byte[] ciphertext) =>
        Convert.ToHexStringLower(SHA256.HashData(ciphertext));

    private static bool FixedTimeEquals(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        var leftBytes = global::System.Text.Encoding.ASCII.GetBytes(left);
        var rightBytes = global::System.Text.Encoding.ASCII.GetBytes(right);

        try
        {
            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(leftBytes);
            CryptographicOperations.ZeroMemory(rightBytes);
        }
    }

    private static MachineSecretEnvelopeException InvalidEnvelope() =>
        new(
            MachineSecretEnvelopeFailure.InvalidEnvelope,
            "The Control Desk machine-secret envelope is unavailable or invalid.");

    private static void DeleteIfPresent(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    private sealed class OuterEnvelope
    {
        public int SchemaVersion { get; init; }

        public string? ProtectionKind { get; init; }

        public string? Product { get; init; }

        public string? Purpose { get; init; }

        public string? Ciphertext { get; init; }

        public string? CiphertextSha256 { get; init; }
    }

    private sealed class ProtectedPayload
    {
        public int SchemaVersion { get; init; }

        public Guid GenerationId { get; init; }

        public DateTimeOffset CreatedAtUtc { get; init; }

        public string? SessionSigningKeyId { get; init; }

        public string? SessionSigningKey { get; init; }
    }
}
