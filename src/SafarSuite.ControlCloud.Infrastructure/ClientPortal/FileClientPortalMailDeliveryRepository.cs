using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileClientPortalMailDeliveryRepository : IClientPortalMailDeliveryRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileClientPortalMailDeliveryRepository(ClientPortalInvitationDeliveryOptions options)
    {
        _storePath = ResolveStorePath(options.MailQueueStorePath);
    }

    public async Task AddAsync(
        ControlCloudClientPortalMailDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var deliveries = await ReadAllAsync(cancellationToken);
            var existing = deliveries.SingleOrDefault(stored => stored.DeliveryId == delivery.DeliveryId);

            if (existing is not null)
            {
                if (IsSameMessage(existing, delivery))
                {
                    return;
                }

                throw new InvalidOperationException(
                    $"Mail delivery '{delivery.DeliveryId}' already exists with different content.");
            }

            deliveries.Add(delivery);
            await WriteAllAsync(deliveries, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyCollection<ControlCloudClientPortalMailDelivery>> ClaimDueAsync(
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        ValidateClaimArguments(leaseDuration, batchSize);
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var deliveries = await ReadAllAsync(cancellationToken);
            var claimed = deliveries
                .Where(delivery => delivery.IsDueAt(nowUtc))
                .OrderBy(delivery => delivery.NextAttemptAtUtc)
                .ThenBy(delivery => delivery.CreatedAtUtc)
                .Take(batchSize)
                .ToArray();

            if (claimed.Length == 0)
            {
                return claimed;
            }

            var leaseExpiresAtUtc = nowUtc.Add(leaseDuration);

            foreach (var delivery in claimed)
            {
                delivery.Claim(Guid.NewGuid(), nowUtc, leaseExpiresAtUtc);
            }

            await WriteAllAsync(deliveries, cancellationToken);
            return claimed;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        ControlCloudClientPortalMailDelivery delivery,
        Guid expectedLeaseId,
        CancellationToken cancellationToken = default)
    {
        if (expectedLeaseId == Guid.Empty)
        {
            throw new InvalidOperationException("Expected mail delivery lease id is required.");
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            var deliveries = await ReadAllAsync(cancellationToken);
            var index = deliveries.FindIndex(stored => stored.DeliveryId == delivery.DeliveryId);

            if (index < 0)
            {
                throw new InvalidOperationException(
                    $"Mail delivery '{delivery.DeliveryId}' was not found.");
            }

            if (deliveries[index].LeaseId != expectedLeaseId || delivery.LeaseId != expectedLeaseId)
            {
                throw new InvalidOperationException(
                    $"Mail delivery '{delivery.DeliveryId}' no longer holds lease '{expectedLeaseId}'.");
            }

            deliveries[index] = delivery;
            await WriteAllAsync(deliveries, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<ControlCloudClientPortalMailDelivery>> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return [];
        }

        await using var stream = new FileStream(
            _storePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 16_384,
            useAsync: true);

        return await JsonSerializer.DeserializeAsync<List<ControlCloudClientPortalMailDelivery>>(
                   stream,
                   JsonOptions,
                   cancellationToken)
               ?? [];
    }

    private async Task WriteAllAsync(
        IReadOnlyCollection<ControlCloudClientPortalMailDelivery> deliveries,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_storePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{_storePath}.tmp";

        await using (var stream = new FileStream(
                         temporaryPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 16_384,
                         useAsync: true))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                deliveries,
                JsonOptions,
                cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(temporaryPath, _storePath, overwrite: true);
    }

    private static bool IsSameMessage(
        ControlCloudClientPortalMailDelivery left,
        ControlCloudClientPortalMailDelivery right)
    {
        return left.ClientId == right.ClientId
            && string.Equals(left.RecipientEmail, right.RecipientEmail, StringComparison.Ordinal)
            && string.Equals(left.RecipientName, right.RecipientName, StringComparison.Ordinal)
            && string.Equals(left.Subject, right.Subject, StringComparison.Ordinal)
            && string.Equals(left.TextBody, right.TextBody, StringComparison.Ordinal)
            && left.CreatedAtUtc == right.CreatedAtUtc;
    }

    private static void ValidateClaimArguments(TimeSpan leaseDuration, int batchSize)
    {
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }
    }

    private static string ResolveStorePath(string configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "App_Data/client-portal-mail-deliveries.json"
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
