using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.InboundControlDesk.Ports;
using SafarSuite.ControlCloud.Domain.Modules.InboundControlDesk;

namespace SafarSuite.ControlCloud.Infrastructure.InboundControlDesk;

public sealed class FileControlDeskEnvelopeReceiptRepository : IControlDeskEnvelopeReceiptRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileControlDeskEnvelopeReceiptRepository(ControlCloudReceiverOptions options)
    {
        _storePath = ResolveStorePath(options.ReceiptStorePath);
    }

    public async Task<ControlDeskEnvelopeReceipt?> GetAcceptedByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_storePath))
            {
                return null;
            }

            await using var stream = new FileStream(
                _storePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var receipt = JsonSerializer.Deserialize<ControlDeskEnvelopeReceiptRecord>(line, JsonOptions);

                if (receipt is null
                    || receipt.Status != ControlDeskEnvelopeReceiptStatus.Accepted.ToString()
                    || !string.Equals(receipt.IdempotencyKey, idempotencyKey, StringComparison.Ordinal))
                {
                    continue;
                }

                return ToAcceptedReceipt(receipt);
            }

            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(
        ControlDeskEnvelopeReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var directory = Path.GetDirectoryName(_storePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = JsonSerializer.Serialize(
                ControlDeskEnvelopeReceiptRecord.FromReceipt(receipt),
                JsonOptions);

            await File.AppendAllTextAsync(
                _storePath,
                line + Environment.NewLine,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static ControlDeskEnvelopeReceipt ToAcceptedReceipt(ControlDeskEnvelopeReceiptRecord receipt)
    {
        return ControlDeskEnvelopeReceipt.Accepted(
            receipt.ReceiptId,
            receipt.MessageId,
            receipt.MessageType,
            receipt.SubjectType,
            receipt.SubjectId,
            receipt.SourceSystem,
            receipt.SourceEnvironment,
            receipt.IdempotencyKey,
            receipt.SignatureKeyId,
            receipt.SignatureValue,
            receipt.CloudReference,
            receipt.OccurredAtUtc,
            receipt.PreparedAtUtc,
            receipt.ReceivedAtUtc);
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/control-cloud-receipts.jsonl"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private sealed record ControlDeskEnvelopeReceiptRecord(
        Guid ReceiptId,
        Guid MessageId,
        string MessageType,
        string SubjectType,
        string SubjectId,
        string SourceSystem,
        string SourceEnvironment,
        string IdempotencyKey,
        string SignatureKeyId,
        string SignatureValue,
        string Status,
        string CloudReference,
        DateTimeOffset OccurredAtUtc,
        DateTimeOffset PreparedAtUtc,
        DateTimeOffset ReceivedAtUtc,
        string? Detail)
    {
        public static ControlDeskEnvelopeReceiptRecord FromReceipt(ControlDeskEnvelopeReceipt receipt)
        {
            return new ControlDeskEnvelopeReceiptRecord(
                receipt.ReceiptId,
                receipt.MessageId,
                receipt.MessageType,
                receipt.SubjectType,
                receipt.SubjectId,
                receipt.SourceSystem,
                receipt.SourceEnvironment,
                receipt.IdempotencyKey,
                receipt.SignatureKeyId,
                receipt.SignatureValue,
                receipt.Status.ToString(),
                receipt.CloudReference,
                receipt.OccurredAtUtc,
                receipt.PreparedAtUtc,
                receipt.ReceivedAtUtc,
                receipt.Detail);
        }
    }
}
