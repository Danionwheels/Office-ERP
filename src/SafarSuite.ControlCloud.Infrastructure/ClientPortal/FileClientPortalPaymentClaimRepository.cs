using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class FileClientPortalPaymentClaimRepository : IClientPortalPaymentClaimRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _storePath;

    public FileClientPortalPaymentClaimRepository(ClientPortalAccessOptions options) =>
        _storePath = Resolve(options.PaymentClaimStorePath);

    public async Task<ControlCloudClientPortalPaymentClaim?> GetByIdAsync(
        Guid claimId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            return (await ReadAsync(cancellationToken))
                .SingleOrDefault(claim => claim.ClaimId == claimId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ControlCloudClientPortalPaymentClaim?> GetByClientAndReferenceAsync(
        Guid clientId,
        string normalizedTransferReferenceNumber,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            return (await ReadAsync(cancellationToken)).SingleOrDefault(claim =>
                claim.ClientId == clientId
                && string.Equals(
                    claim.NormalizedTransferReferenceNumber,
                    normalizedTransferReferenceNumber,
                    StringComparison.Ordinal));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyCollection<ControlCloudClientPortalPaymentClaim>> ListAsync(
        Guid? clientId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            return (await ReadAsync(cancellationToken))
                .Where(claim => clientId is null || claim.ClientId == clientId.Value)
                .OrderByDescending(claim => claim.SubmittedAtUtc)
                .ThenByDescending(claim => claim.ClaimId)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(
        ControlCloudClientPortalPaymentClaim claim,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var claims = await ReadAsync(cancellationToken);
            if (claims.Any(stored => stored.ClaimId == claim.ClaimId))
            {
                throw new InvalidOperationException($"Payment claim '{claim.ClaimId}' already exists.");
            }

            if (claims.Any(stored =>
                    stored.ClientId == claim.ClientId
                    && string.Equals(
                        stored.NormalizedTransferReferenceNumber,
                        claim.NormalizedTransferReferenceNumber,
                        StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "A payment claim already uses this transfer reference.");
            }

            claim.OriginalConcurrencyToken = claim.ConcurrencyToken;
            claims.Add(claim);
            await WriteAsync(claims, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        ControlCloudClientPortalPaymentClaim claim,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var claims = await ReadAsync(cancellationToken);
            var index = claims.FindIndex(stored => stored.ClaimId == claim.ClaimId);
            if (index < 0)
            {
                throw new InvalidOperationException($"Payment claim '{claim.ClaimId}' was not found.");
            }

            if (claims[index].ConcurrencyToken != claim.OriginalConcurrencyToken)
            {
                throw new InvalidOperationException("Payment claim state changed concurrently.");
            }

            claim.OriginalConcurrencyToken = claim.ConcurrencyToken;
            claims[index] = claim;
            await WriteAsync(claims, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<ControlCloudClientPortalPaymentClaim>> ReadAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_storePath);
        var claims = await JsonSerializer.DeserializeAsync<List<ControlCloudClientPortalPaymentClaim>>(
            stream,
            JsonOptions,
            cancellationToken) ?? [];

        foreach (var claim in claims)
        {
            claim.OriginalConcurrencyToken = claim.ConcurrencyToken;
        }

        return claims;
    }

    private async Task WriteAsync(
        List<ControlCloudClientPortalPaymentClaim> claims,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
        await using var stream = new FileStream(
            _storePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, claims, JsonOptions, cancellationToken);
    }

    private static string Resolve(string path) => Path.IsPathRooted(path)
        ? path
        : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
}
