using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Api.Modules.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public sealed class EfProviderAccessOperatorStore : IProviderAccessOperatorStore
{
    private static readonly SemaphoreSlim SeedGate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ControlCloudDbContext _dbContext;
    private readonly ClientPortalProviderAccessOptions _options;

    public EfProviderAccessOperatorStore(
        ControlCloudDbContext dbContext,
        ClientPortalProviderAccessOptions options)
    {
        _dbContext = dbContext;
        _options = options;
    }

    public async Task<IReadOnlyCollection<ProviderAccessOperator>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);

        return await _dbContext.ProviderAccessOperators
            .AsNoTracking()
            .OrderBy(providerOperator => providerOperator.Email)
            .Select(providerOperator => ToModel(providerOperator))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ProviderAccessOperator?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);

        var normalizedUserId = userId.Trim();
        var entity = await _dbContext.ProviderAccessOperators
            .AsNoTracking()
            .SingleOrDefaultAsync(
                providerOperator => providerOperator.UserId == normalizedUserId,
                cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<ProviderAccessOperator?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);

        var normalizedEmail = NormalizeEmail(email);
        var entity = await _dbContext.ProviderAccessOperators
            .AsNoTracking()
            .SingleOrDefaultAsync(
                providerOperator => providerOperator.NormalizedEmail == normalizedEmail,
                cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task SaveAsync(
        ProviderAccessOperator providerOperator,
        CancellationToken cancellationToken = default)
    {
        EnsureSupportedScopes(providerOperator.Scopes, providerOperator.Email);

        await EnsureSeededAsync(cancellationToken);

        var normalizedEmail = NormalizeEmail(providerOperator.Email);
        var entity = await _dbContext.ProviderAccessOperators
            .SingleOrDefaultAsync(
                stored => stored.UserId == providerOperator.UserId,
                cancellationToken);

        if (entity is null)
        {
            entity = await _dbContext.ProviderAccessOperators
                .SingleOrDefaultAsync(
                    stored => stored.NormalizedEmail == normalizedEmail,
                    cancellationToken);

            if (entity is not null
                && !entity.UserId.Equals(providerOperator.UserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "A provider access operator already exists for this email address.");
            }
        }

        if (entity is null)
        {
            entity = new ControlCloudProviderAccessOperatorEntity();
            await _dbContext.ProviderAccessOperators.AddAsync(entity, cancellationToken);
        }

        Apply(providerOperator, entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSeededAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.ProviderAccessOperators.AnyAsync(cancellationToken))
        {
            return;
        }

        await SeedGate.WaitAsync(cancellationToken);

        try
        {
            if (await _dbContext.ProviderAccessOperators.AnyAsync(cancellationToken))
            {
                return;
            }

            var seededOperators = CreateSeededOperators().ToArray();

            if (seededOperators.Length == 0)
            {
                return;
            }

            await _dbContext.ProviderAccessOperators.AddRangeAsync(seededOperators, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            SeedGate.Release();
        }
    }

    private IEnumerable<ControlCloudProviderAccessOperatorEntity> CreateSeededOperators()
    {
        var now = DateTimeOffset.UtcNow;

        return _options.Users
            .Where(user =>
                !string.IsNullOrWhiteSpace(user.Email)
                && !string.IsNullOrWhiteSpace(user.PasswordHash))
            .GroupBy(user => NormalizeEmail(user.Email), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(user =>
            {
                var providerOperator = new ProviderAccessOperator
                {
                    UserId = string.IsNullOrWhiteSpace(user.UserId)
                        ? Guid.NewGuid().ToString("N")
                        : user.UserId.Trim(),
                    Email = NormalizeEmail(user.Email),
                    FullName = user.FullName.Trim(),
                    PasswordHash = user.PasswordHash.Trim(),
                    Status = ProviderAccessOperatorStatuses.IsSupported(user.Status)
                        ? user.Status.Trim()
                        : ProviderAccessOperatorStatuses.Suspended,
                    Scopes = NormalizeSeedScopes(user).ToArray(),
                    CreatedAtUtc = now,
                    CreatedBy = "configuration-seed"
                };
                var entity = new ControlCloudProviderAccessOperatorEntity();
                Apply(providerOperator, entity);

                return entity;
            });
    }

    private static ProviderAccessOperator ToModel(ControlCloudProviderAccessOperatorEntity entity)
    {
        return new ProviderAccessOperator
        {
            UserId = entity.UserId,
            Email = entity.Email,
            FullName = entity.FullName,
            PasswordHash = entity.PasswordHash,
            Status = entity.Status,
            Scopes = DeserializeScopes(entity.ScopesJson).ToArray(),
            RecoveryCodeHashes = DeserializeRecoveryCodeHashes(entity.RecoveryCodeHashesJson).ToArray(),
            RecoveryCodesUpdatedAtUtc = entity.RecoveryCodesUpdatedAtUtc,
            RecoveryCodesUpdatedBy = entity.RecoveryCodesUpdatedBy,
            LastRecoveryCodeUsedAtUtc = entity.LastRecoveryCodeUsedAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            CreatedBy = entity.CreatedBy,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            UpdatedBy = entity.UpdatedBy,
            LastLoginAtUtc = entity.LastLoginAtUtc
        };
    }

    private static void Apply(
        ProviderAccessOperator providerOperator,
        ControlCloudProviderAccessOperatorEntity entity)
    {
        entity.UserId = providerOperator.UserId.Trim();
        entity.Email = NormalizeEmail(providerOperator.Email);
        entity.NormalizedEmail = NormalizeEmail(providerOperator.Email);
        entity.FullName = providerOperator.FullName.Trim();
        entity.PasswordHash = providerOperator.PasswordHash.Trim();
        entity.Status = providerOperator.Status.Trim();
        entity.ScopesJson = JsonSerializer.Serialize(
            ProviderAccessScopes.Normalize(providerOperator.Scopes, []),
            JsonOptions);
        entity.RecoveryCodeHashesJson = JsonSerializer.Serialize(
            NormalizeRecoveryCodeHashes(providerOperator.RecoveryCodeHashes),
            JsonOptions);
        entity.RecoveryCodesUpdatedAtUtc = providerOperator.RecoveryCodesUpdatedAtUtc;
        entity.RecoveryCodesUpdatedBy = string.IsNullOrWhiteSpace(providerOperator.RecoveryCodesUpdatedBy)
            ? null
            : providerOperator.RecoveryCodesUpdatedBy.Trim();
        entity.LastRecoveryCodeUsedAtUtc = providerOperator.LastRecoveryCodeUsedAtUtc;
        entity.CreatedAtUtc = providerOperator.CreatedAtUtc;
        entity.CreatedBy = providerOperator.CreatedBy.Trim();
        entity.UpdatedAtUtc = providerOperator.UpdatedAtUtc;
        entity.UpdatedBy = string.IsNullOrWhiteSpace(providerOperator.UpdatedBy)
            ? null
            : providerOperator.UpdatedBy.Trim();
        entity.LastLoginAtUtc = providerOperator.LastLoginAtUtc;
    }

    private static IReadOnlyCollection<string> DeserializeScopes(string scopesJson)
    {
        if (string.IsNullOrWhiteSpace(scopesJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(scopesJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? ""
            : email.Trim().ToLowerInvariant();
    }

    private static IReadOnlyCollection<string> DeserializeRecoveryCodeHashes(string hashesJson)
    {
        if (string.IsNullOrWhiteSpace(hashesJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(hashesJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string[] NormalizeRecoveryCodeHashes(IEnumerable<string>? recoveryCodeHashes)
    {
        return (recoveryCodeHashes ?? [])
            .Select(hash => hash.Trim())
            .Where(hash => !string.IsNullOrWhiteSpace(hash))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private IReadOnlyCollection<string> NormalizeSeedScopes(ProviderAccessUserOptions user)
    {
        var scopes = ProviderAccessScopes.Normalize(user.Scopes, _options.DefaultScopes);
        EnsureSupportedScopes(scopes, user.Email);

        return scopes;
    }

    private static void EnsureSupportedScopes(
        IEnumerable<string>? scopes,
        string operatorEmail)
    {
        var unsupportedScopes = ProviderAccessScopes.FindUnsupported(scopes);

        if (unsupportedScopes.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Provider access operator '{NormalizeEmail(operatorEmail)}' has unsupported scope(s): {string.Join(", ", unsupportedScopes)}.");
    }
}
