using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Api.Modules.ClientPortal;
using SafarSuite.ControlCloud.Api.Modules.ProviderAccess;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Infrastructure;
using SafarSuite.ControlCloud.Infrastructure.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.InboundControlDesk;
using SafarSuite.ControlCloud.Infrastructure.LocalServer;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;

var options = SmokeOptions.Parse(args);

if (options.ShowHelp)
{
    Console.WriteLine(SmokeOptions.HelpText);
    return 0;
}

var checks = new List<string>();
var workDirectory = Path.Combine(
    options.WorkDirectory,
    $"provider-access-smoke-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}");

Directory.CreateDirectory(workDirectory);

try
{
    var credentials = new HmacClientPortalCredentialService(new ClientPortalAccessOptions());
    var clock = new FixedClock(DateTimeOffset.Parse("2026-07-07T00:00:00Z"));
    var providerOptions = new ClientPortalProviderAccessOptions
    {
        SharedSecret = "provider-access-smoke-secret",
        SessionSigningSecret = "provider-access-smoke-session-signing-secret",
        SessionMinutes = 30,
        FailedLoginLockoutThreshold = 3,
        FailedLoginLockoutMinutes = 15,
        OperatorStorePath = Path.Combine(workDirectory, "provider-access-operators.json"),
        Users =
        [
            new ProviderAccessUserOptions
            {
                UserId = "seed-provider-admin",
                Email = "seed.provider@safarsuite.local",
                FullName = "Seed Provider Admin",
                PasswordHash = credentials.HashPassword("SeedProviderPass123!"),
                Status = ProviderAccessOperatorStatuses.Active,
                Scopes =
                [
                    ProviderAccessScopes.AppActivationRead,
                    ProviderAccessScopes.ProviderOperatorsManage
                ]
            }
        ]
    };

    var totpProtector = new ProviderAccessTotpSecretProtector(providerOptions);
    var fileStore = new FileProviderAccessOperatorStore(providerOptions);
    var seededOperators = await fileStore.ListAsync();
    Require(seededOperators.Count == 1, "file store should seed one provider operator.");
    Require(File.Exists(providerOptions.OperatorStorePath), "file store should persist seeded operators.");
    checks.Add("seeded provider operator into file-backed store");

    var seededOperator = seededOperators.Single();
    Require(
        seededOperator.Scopes.Contains(ProviderAccessScopes.ProviderOperatorsManage),
        "seeded operator should keep provider-operators scope.");

    var sessionService = new ProviderAccessSessionService(
        providerOptions,
        clock,
        credentials,
        fileStore,
        totpProtector);
    var session = await sessionService.CreateSessionFromCredentialsAsync(
        "seed.provider@safarsuite.local",
        "SeedProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10);

    Require(session.IsSuccess, "valid provider operator credentials should issue a session.");
    Require(session.Scopes?.Single() == ProviderAccessScopes.ProviderOperatorsManage, "session should carry requested scope.");
    checks.Add("issued scoped provider operator session");

    var recoveryCode = "ABCD-EFGH-JKLM";
    var recoveryOperator = new ProviderAccessOperator
    {
        UserId = Guid.NewGuid().ToString("N"),
        Email = "mfa.provider@safarsuite.local",
        FullName = "MFA Provider",
        PasswordHash = credentials.HashPassword("MfaProviderPass123!"),
        Status = ProviderAccessOperatorStatuses.Active,
        Scopes = [ProviderAccessScopes.ProviderOperatorsManage],
        RecoveryCodeHashes = [HashRecoveryCode(credentials, recoveryCode)],
        RecoveryCodesUpdatedAtUtc = clock.UtcNow,
        RecoveryCodesUpdatedBy = "provider-access-smoke",
        CreatedAtUtc = clock.UtcNow,
        CreatedBy = "provider-access-smoke"
    };

    await fileStore.SaveAsync(recoveryOperator);

    var mfaRequiredSession = await sessionService.CreateSessionFromCredentialsAsync(
        "mfa.provider@safarsuite.local",
        "MfaProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10);
    var invalidMfaSession = await sessionService.CreateSessionFromCredentialsAsync(
        "mfa.provider@safarsuite.local",
        "MfaProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10,
        recoveryCode: "wrong-code");
    var mfaSession = await sessionService.CreateSessionFromCredentialsAsync(
        "mfa.provider@safarsuite.local",
        "MfaProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10,
        recoveryCode: recoveryCode.ToLowerInvariant());
    var exhaustedMfaSession = await sessionService.CreateSessionFromCredentialsAsync(
        "mfa.provider@safarsuite.local",
        "MfaProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10,
        recoveryCode: recoveryCode);
    var consumedRecoveryOperator = await fileStore.GetByEmailAsync("mfa.provider@safarsuite.local");

    Require(!mfaRequiredSession.IsSuccess, "MFA-enabled operator should require a recovery code.");
    Require(mfaRequiredSession.FailureCode == "ProviderMfaRequired", "missing recovery code should return ProviderMfaRequired.");
    Require(!invalidMfaSession.IsSuccess, "MFA-enabled operator should reject an invalid recovery code.");
    Require(invalidMfaSession.FailureCode == "ProviderMfaInvalid", "invalid recovery code should return ProviderMfaInvalid.");
    Require(mfaSession.IsSuccess, "MFA-enabled operator should issue a session with a valid recovery code.");
    Require(!exhaustedMfaSession.IsSuccess, "MFA-enabled operator should not fall back to password-only after all recovery codes are consumed.");
    Require(exhaustedMfaSession.FailureCode == "ProviderMfaUnavailable", "exhausted recovery codes should return ProviderMfaUnavailable.");
    Require(consumedRecoveryOperator?.RecoveryCodeHashes.Length == 0, "valid recovery code should be consumed.");
    Require(consumedRecoveryOperator?.LastRecoveryCodeUsedAtUtc == clock.UtcNow, "recovery code use should be timestamped.");
    checks.Add("enforced and consumed provider recovery-code MFA");

    var totpSecret = ProviderAccessTotp.CreateSecret();
    var totpCode = ProviderAccessTotp.CreateCode(totpSecret, clock.UtcNow);
    var totpOperator = new ProviderAccessOperator
    {
        UserId = Guid.NewGuid().ToString("N"),
        Email = "totp.provider@safarsuite.local",
        FullName = "TOTP Provider",
        PasswordHash = credentials.HashPassword("TotpProviderPass123!"),
        Status = ProviderAccessOperatorStatuses.Active,
        Scopes = [ProviderAccessScopes.ProviderOperatorsManage],
        TotpSecret = totpProtector.Protect(totpSecret),
        TotpEnabledAtUtc = clock.UtcNow,
        TotpUpdatedAtUtc = clock.UtcNow,
        TotpUpdatedBy = "provider-access-smoke",
        CreatedAtUtc = clock.UtcNow,
        CreatedBy = "provider-access-smoke"
    };

    await fileStore.SaveAsync(totpOperator);

    var totpRequiredSession = await sessionService.CreateSessionFromCredentialsAsync(
        "totp.provider@safarsuite.local",
        "TotpProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10);
    var invalidTotpSession = await sessionService.CreateSessionFromCredentialsAsync(
        "totp.provider@safarsuite.local",
        "TotpProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10,
        totpCode: "000000");
    var totpSession = await sessionService.CreateSessionFromCredentialsAsync(
        "totp.provider@safarsuite.local",
        "TotpProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10,
        totpCode: totpCode);
    var replayedTotpSession = await sessionService.CreateSessionFromCredentialsAsync(
        "totp.provider@safarsuite.local",
        "TotpProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10,
        totpCode: totpCode);
    var usedTotpOperator = await fileStore.GetByEmailAsync("totp.provider@safarsuite.local");

    Require(!totpRequiredSession.IsSuccess, "TOTP-enabled operator should require an MFA code.");
    Require(totpRequiredSession.FailureCode == "ProviderMfaRequired", "missing TOTP code should return ProviderMfaRequired.");
    Require(!invalidTotpSession.IsSuccess, "TOTP-enabled operator should reject an invalid TOTP code.");
    Require(invalidTotpSession.FailureCode == "ProviderMfaInvalid", "invalid TOTP code should return ProviderMfaInvalid.");
    Require(totpSession.IsSuccess, "TOTP-enabled operator should issue a session with a valid TOTP code.");
    Require(!replayedTotpSession.IsSuccess, "TOTP-enabled operator should reject a replayed TOTP code.");
    Require(replayedTotpSession.FailureCode == "ProviderMfaInvalid", "replayed TOTP code should return ProviderMfaInvalid.");
    Require(usedTotpOperator?.TotpSecret != totpSecret, "stored TOTP secret should not be plaintext.");
    Require(totpProtector.IsProtected(usedTotpOperator?.TotpSecret ?? ""), "stored TOTP secret should use the protected payload format.");
    Require(usedTotpOperator?.LastTotpUsedAtUtc == clock.UtcNow, "TOTP use should be timestamped.");
    Require(usedTotpOperator?.LastTotpStep is not null, "TOTP use should store the accepted step.");
    checks.Add("enforced provider TOTP MFA, replay guard, and protected custody");

    var legacyTotpSecret = ProviderAccessTotp.CreateSecret();
    var legacyTotpOperator = new ProviderAccessOperator
    {
        UserId = Guid.NewGuid().ToString("N"),
        Email = "legacy-totp.provider@safarsuite.local",
        FullName = "Legacy TOTP Provider",
        PasswordHash = credentials.HashPassword("LegacyTotpPass123!"),
        Status = ProviderAccessOperatorStatuses.Active,
        Scopes = [ProviderAccessScopes.ProviderOperatorsManage],
        TotpSecret = legacyTotpSecret,
        TotpEnabledAtUtc = clock.UtcNow,
        TotpUpdatedAtUtc = clock.UtcNow,
        TotpUpdatedBy = "provider-access-smoke",
        CreatedAtUtc = clock.UtcNow,
        CreatedBy = "provider-access-smoke"
    };

    await fileStore.SaveAsync(legacyTotpOperator);

    var legacyTotpSession = await sessionService.CreateSessionFromCredentialsAsync(
        "legacy-totp.provider@safarsuite.local",
        "LegacyTotpPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10,
        totpCode: ProviderAccessTotp.CreateCode(legacyTotpSecret, clock.UtcNow));
    var migratedLegacyTotpOperator = await fileStore.GetByEmailAsync("legacy-totp.provider@safarsuite.local");

    Require(legacyTotpSession.IsSuccess, "legacy plaintext TOTP secret should remain usable during migration.");
    Require(
        totpProtector.IsProtected(migratedLegacyTotpOperator?.TotpSecret ?? ""),
        "legacy plaintext TOTP secret should be rewritten as protected material after login.");
    checks.Add("migrated legacy plaintext provider TOTP secret on login");

    var resetAfterFailureOperator = new ProviderAccessOperator
    {
        UserId = Guid.NewGuid().ToString("N"),
        Email = "login-reset.provider@safarsuite.local",
        FullName = "Login Reset Provider",
        PasswordHash = credentials.HashPassword("LoginResetPass123!"),
        Status = ProviderAccessOperatorStatuses.Active,
        Scopes = [ProviderAccessScopes.ProviderOperatorsManage],
        CreatedAtUtc = clock.UtcNow,
        CreatedBy = "provider-access-smoke"
    };

    await fileStore.SaveAsync(resetAfterFailureOperator);

    var failedPasswordSession = await sessionService.CreateSessionFromCredentialsAsync(
        "login-reset.provider@safarsuite.local",
        "wrong-password",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10);
    var successfulAfterFailureSession = await sessionService.CreateSessionFromCredentialsAsync(
        "login-reset.provider@safarsuite.local",
        "LoginResetPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10);
    var resetAfterFailureState = await fileStore.GetByEmailAsync("login-reset.provider@safarsuite.local");

    Require(!failedPasswordSession.IsSuccess, "bad provider operator password should fail.");
    Require(failedPasswordSession.FailureCode == "ProviderCredentialsInvalid", "bad password should return ProviderCredentialsInvalid before lockout threshold.");
    Require(successfulAfterFailureSession.IsSuccess, "valid provider operator login should succeed after a non-locking failed attempt.");
    Require(resetAfterFailureState?.FailedLoginAttemptCount == 0, "successful provider login should clear failed-login count.");
    Require(resetAfterFailureState?.LastFailedLoginAtUtc is null, "successful provider login should clear failed-login timestamp.");
    Require(resetAfterFailureState?.LockoutEndsAtUtc is null, "successful provider login should clear lockout state.");
    checks.Add("reset provider login failure counters after successful login");

    var lockoutSecret = ProviderAccessTotp.CreateSecret();
    var lockoutOperator = new ProviderAccessOperator
    {
        UserId = Guid.NewGuid().ToString("N"),
        Email = "lockout.provider@safarsuite.local",
        FullName = "Lockout Provider",
        PasswordHash = credentials.HashPassword("LockoutProviderPass123!"),
        Status = ProviderAccessOperatorStatuses.Active,
        Scopes = [ProviderAccessScopes.ProviderOperatorsManage],
        TotpSecret = totpProtector.Protect(lockoutSecret),
        TotpEnabledAtUtc = clock.UtcNow,
        TotpUpdatedAtUtc = clock.UtcNow,
        TotpUpdatedBy = "provider-access-smoke",
        CreatedAtUtc = clock.UtcNow,
        CreatedBy = "provider-access-smoke"
    };

    await fileStore.SaveAsync(lockoutOperator);

    var firstBadMfaSession = await sessionService.CreateSessionFromCredentialsAsync(
        "lockout.provider@safarsuite.local",
        "LockoutProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10,
        totpCode: "000000");
    var secondBadMfaSession = await sessionService.CreateSessionFromCredentialsAsync(
        "lockout.provider@safarsuite.local",
        "LockoutProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10,
        totpCode: "111111");
    var lockingBadMfaSession = await sessionService.CreateSessionFromCredentialsAsync(
        "lockout.provider@safarsuite.local",
        "LockoutProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10,
        totpCode: "222222");
    var lockedGoodMfaSession = await sessionService.CreateSessionFromCredentialsAsync(
        "lockout.provider@safarsuite.local",
        "LockoutProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10,
        totpCode: ProviderAccessTotp.CreateCode(lockoutSecret, clock.UtcNow));
    var lockedOperatorState = await fileStore.GetByEmailAsync("lockout.provider@safarsuite.local");

    Require(firstBadMfaSession.FailureCode == "ProviderMfaInvalid", "first bad MFA attempt should return ProviderMfaInvalid.");
    Require(secondBadMfaSession.FailureCode == "ProviderMfaInvalid", "second bad MFA attempt should return ProviderMfaInvalid.");
    Require(lockingBadMfaSession.FailureCode == "ProviderLoginLocked", "threshold MFA failure should lock provider login.");
    Require(lockedGoodMfaSession.FailureCode == "ProviderLoginLocked", "valid MFA should be blocked while provider login is locked.");
    Require(lockedOperatorState?.FailedLoginAttemptCount == 3, "locked provider should store failed-login count.");
    Require(lockedOperatorState?.LastFailedLoginAtUtc == clock.UtcNow, "locked provider should store last failed login timestamp.");
    Require(lockedOperatorState?.LockoutEndsAtUtc == clock.UtcNow.AddMinutes(15), "locked provider should store lockout expiry.");
    checks.Add("locked provider login after repeated MFA failures");

    var rotatedProviderOptions = CopyOptionsWithSessionSigningKeys(
        providerOptions,
        activeKeyId: "provider-access-smoke-session-signing-key-v2",
        [
            new ProviderAccessSessionSigningKeyOptions
            {
                KeyId = "provider-access-smoke-session-signing-key-v2",
                Secret = "provider-access-smoke-session-signing-secret-v2"
            },
            new ProviderAccessSessionSigningKeyOptions
            {
                KeyId = "provider-access-smoke-session-signing-key-v1",
                Secret = providerOptions.SessionSigningSecret
            }
        ]);
    var rotatedSessionService = new ProviderAccessSessionService(
        rotatedProviderOptions,
        clock,
        credentials,
        fileStore,
        new ProviderAccessTotpSecretProtector(rotatedProviderOptions));
    var rotatedAuthorization = AuthorizeBearerToken(
        rotatedSessionService,
        session.AccessToken!,
        ProviderAccessScopes.ProviderOperatorsManage);

    Require(rotatedAuthorization.IsSuccess, "rotated signing key ring should accept sessions signed by a previous key.");
    checks.Add("accepted provider session signed by previous signing key");

    var retiredProviderOptions = CopyOptionsWithSessionSigningKeys(
        providerOptions,
        activeKeyId: "provider-access-smoke-session-signing-key-v2",
        [
            new ProviderAccessSessionSigningKeyOptions
            {
                KeyId = "provider-access-smoke-session-signing-key-v2",
                Secret = "provider-access-smoke-session-signing-secret-v2"
            }
        ]);
    var retiredSessionService = new ProviderAccessSessionService(
        retiredProviderOptions,
        clock,
        credentials,
        fileStore,
        new ProviderAccessTotpSecretProtector(retiredProviderOptions));
    var retiredAuthorization = AuthorizeBearerToken(
        retiredSessionService,
        session.AccessToken!,
        ProviderAccessScopes.ProviderOperatorsManage);

    Require(!retiredAuthorization.IsSuccess, "retired signing key ring should reject sessions signed by a removed key.");
    Require(retiredAuthorization.FailureCode == "ProviderAccessDenied", "retired signing key should return a denied authorization failure.");
    checks.Add("rejected provider session after previous signing key removal");

    var fileSourcedProviderOptions = CreateFileSourcedOptions(workDirectory, providerOptions.OperatorStorePath);
    var fileSourcedTotpProtector = new ProviderAccessTotpSecretProtector(fileSourcedProviderOptions);
    var fileSourcedSessionService = new ProviderAccessSessionService(
        fileSourcedProviderOptions,
        clock,
        credentials,
        fileStore,
        fileSourcedTotpProtector);
    var fileSourcedSession = fileSourcedSessionService.CreateSession(
        "provider-access-smoke-file-shared-secret",
        "provider-access-smoke-file-custody",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10);
    var fileSourcedAuthorization = AuthorizeBearerToken(
        fileSourcedSessionService,
        fileSourcedSession.AccessToken!,
        ProviderAccessScopes.ProviderOperatorsManage);

    Require(fileSourcedSession.IsSuccess, "file-sourced provider access secrets should issue a shared-secret session.");
    Require(fileSourcedAuthorization.IsSuccess, "file-sourced provider signing key should authorize the issued session.");
    Require(
        fileSourcedTotpProtector.TryUnprotect(fileSourcedTotpProtector.Protect(totpSecret), out var fileSourcedTotpSecret)
        && fileSourcedTotpSecret == totpSecret,
        "file-sourced provider TOTP protection secret should round-trip protected secrets.");
    checks.Add("loaded provider access shared, signing, and TOTP protection secrets from secret files");

    VerifyCloudFileSourcedSecrets(workDirectory, checks);

    var deniedSession = await sessionService.CreateSessionFromCredentialsAsync(
        "seed.provider@safarsuite.local",
        "SeedProviderPass123!",
        [ProviderAccessScopes.ClientPortalManage],
        expiresInMinutes: 10);

    Require(!deniedSession.IsSuccess, "operator should not be able to request an unassigned scope.");
    Require(deniedSession.FailureCode == "ProviderAccessScopeDenied", "unassigned scope should be denied.");
    checks.Add("rejected over-scoped provider operator session");

    var unsupportedSession = await sessionService.CreateSessionFromCredentialsAsync(
        "seed.provider@safarsuite.local",
        "SeedProviderPass123!",
        ["billing:superuser"],
        expiresInMinutes: 10);

    Require(!unsupportedSession.IsSuccess, "unsupported requested scope should be rejected.");
    Require(unsupportedSession.FailureCode == "ProviderAccessScopeUnsupported", "unsupported session scope should return the right code.");
    checks.Add("rejected unsupported requested provider scope");

    var createdOperator = new ProviderAccessOperator
    {
        UserId = Guid.NewGuid().ToString("N"),
        Email = "ops.two@safarsuite.local",
        FullName = "Ops Two",
        PasswordHash = credentials.HashPassword("OpsTwoPassword123!"),
        Status = ProviderAccessOperatorStatuses.Active,
        Scopes = [ProviderAccessScopes.ClientPortalManage],
        CreatedAtUtc = clock.UtcNow,
        CreatedBy = "provider-access-smoke"
    };

    await fileStore.SaveAsync(createdOperator);

    var reloadedStore = new FileProviderAccessOperatorStore(providerOptions);
    var reloadedOperator = await reloadedStore.GetByEmailAsync("OPS.TWO@SAFARSUITE.LOCAL");
    Require(reloadedOperator is not null, "saved provider operator should reload from file store.");
    Require(reloadedOperator!.Scopes.Single() == ProviderAccessScopes.ClientPortalManage, "reloaded operator should keep saved scope.");
    checks.Add("saved and reloaded provider operator from file-backed store");

    await RequireThrowsAsync<InvalidOperationException>(
        () => fileStore.SaveAsync(CopyOperatorWithScopes(createdOperator, ["billing:superuser"])),
        "file store should reject unsupported saved scopes.");
    checks.Add("blocked unsupported scope at file-store boundary");

    using var dbContext = CreateEfDbContext();
    var providerOperatorEntity = dbContext.Model.FindEntityType(
        "SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities.ControlCloudProviderAccessOperatorEntity");

    var entityType = providerOperatorEntity
        ?? throw new InvalidOperationException("EF model should include provider access operator entity.");

    Require(entityType.GetTableName() == "provider_access_operators", "EF provider operator table should be named provider_access_operators.");
    Require(entityType.GetSchema() == "cloud", "EF provider operator table should use cloud schema.");
    Require(
        entityType.GetIndexes().Any(index =>
            index.IsUnique
            && index.GetDatabaseName() == "ux_provider_access_operators_email"),
        "EF provider operator model should keep unique normalized email index.");
    Require(
        entityType.FindProperty("RecoveryCodeHashesJson")?.GetColumnName() == "recovery_code_hashes_json",
        "EF provider operator model should map recovery code hashes.");
    Require(
        entityType.FindProperty("TotpSecret")?.GetColumnName() == "totp_secret",
        "EF provider operator model should map TOTP secrets.");
    Require(
        entityType.FindProperty("FailedLoginAttemptCount")?.GetColumnName() == "failed_login_attempt_count",
        "EF provider operator model should map failed-login counters.");
    checks.Add("verified EF provider operator model mapping");

    var efStore = new EfProviderAccessOperatorStore(dbContext, providerOptions);
    await RequireThrowsAsync<InvalidOperationException>(
        () => efStore.SaveAsync(CopyOperatorWithScopes(createdOperator, ["billing:superuser"])),
        "EF store should reject unsupported saved scopes before database access.");
    checks.Add("blocked unsupported scope at EF-store boundary");
}
finally
{
    if (!options.KeepArtifacts && Directory.Exists(workDirectory))
    {
        Directory.Delete(workDirectory, recursive: true);
    }
}

Console.WriteLine($"Provider access smoke passed {checks.Count} checks:");
foreach (var check in checks)
{
    Console.WriteLine($"- {check}");
}

return 0;

static ControlCloudDbContext CreateEfDbContext()
{
    var options = new DbContextOptionsBuilder<ControlCloudDbContext>()
        .UseNpgsql(
            "Host=localhost;Database=safarsuite_provider_access_smoke;Username=safarsuite;Password=safarsuite",
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "cloud"))
        .Options;

    return new ControlCloudDbContext(options);
}

static ProviderAccessOperator CopyOperatorWithScopes(
    ProviderAccessOperator source,
    string[] scopes)
{
    return new ProviderAccessOperator
    {
        UserId = source.UserId,
        Email = source.Email,
        FullName = source.FullName,
        PasswordHash = source.PasswordHash,
        Status = source.Status,
        Scopes = scopes,
        RecoveryCodeHashes = source.RecoveryCodeHashes,
        RecoveryCodesUpdatedAtUtc = source.RecoveryCodesUpdatedAtUtc,
        RecoveryCodesUpdatedBy = source.RecoveryCodesUpdatedBy,
        LastRecoveryCodeUsedAtUtc = source.LastRecoveryCodeUsedAtUtc,
        TotpSecret = source.TotpSecret,
        TotpEnabledAtUtc = source.TotpEnabledAtUtc,
        TotpUpdatedAtUtc = source.TotpUpdatedAtUtc,
        TotpUpdatedBy = source.TotpUpdatedBy,
        LastTotpUsedAtUtc = source.LastTotpUsedAtUtc,
        LastTotpStep = source.LastTotpStep,
        FailedLoginAttemptCount = source.FailedLoginAttemptCount,
        LastFailedLoginAtUtc = source.LastFailedLoginAtUtc,
        LockoutEndsAtUtc = source.LockoutEndsAtUtc,
        CreatedAtUtc = source.CreatedAtUtc,
        CreatedBy = source.CreatedBy,
        UpdatedAtUtc = source.UpdatedAtUtc,
        UpdatedBy = source.UpdatedBy,
        LastLoginAtUtc = source.LastLoginAtUtc
    };
}

static string HashRecoveryCode(
    IClientPortalCredentialService credentials,
    string recoveryCode)
{
    var normalized = new string(recoveryCode
        .Where(character => !char.IsWhiteSpace(character) && character != '-')
        .Select(character => char.ToUpperInvariant(character))
        .ToArray());

    return credentials.HashSecret($"provider-access-recovery-code:{normalized}");
}

static ClientPortalProviderAccessOptions CreateFileSourcedOptions(
    string workDirectory,
    string operatorStorePath)
{
    var sharedSecretFile = Path.Combine(workDirectory, "provider-access-shared-secret.txt");
    var activeSigningKeyFile = Path.Combine(workDirectory, "provider-access-session-signing-key-v2.txt");
    var previousSigningKeyFile = Path.Combine(workDirectory, "provider-access-session-signing-key-v1.txt");
    var totpProtectionSecretFile = Path.Combine(workDirectory, "provider-access-totp-protection-secret.txt");

    File.WriteAllText(sharedSecretFile, "provider-access-smoke-file-shared-secret");
    File.WriteAllText(activeSigningKeyFile, "provider-access-smoke-file-session-signing-secret-v2");
    File.WriteAllText(previousSigningKeyFile, "provider-access-smoke-file-session-signing-secret-v1");
    File.WriteAllText(totpProtectionSecretFile, "provider-access-smoke-file-totp-protection-secret");

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ClientPortal:ProviderAccess:SharedSecret"] = "inline-provider-secret-should-not-be-used",
            ["ClientPortal:ProviderAccess:SharedSecretFile"] = sharedSecretFile,
            ["ClientPortal:ProviderAccess:ActiveSessionSigningKeyId"] = "provider-access-smoke-file-key-v2",
            ["ClientPortal:ProviderAccess:SessionSigningKeys:0:KeyId"] = "provider-access-smoke-file-key-v2",
            ["ClientPortal:ProviderAccess:SessionSigningKeys:0:SecretFile"] = activeSigningKeyFile,
            ["ClientPortal:ProviderAccess:SessionSigningKeys:1:KeyId"] = "provider-access-smoke-file-key-v1",
            ["ClientPortal:ProviderAccess:SessionSigningKeys:1:SecretFile"] = previousSigningKeyFile,
            ["ClientPortal:ProviderAccess:TotpProtectionSecret"] = "inline-provider-totp-secret-should-not-be-used",
            ["ClientPortal:ProviderAccess:TotpProtectionSecretFile"] = totpProtectionSecretFile,
            ["ClientPortal:ProviderAccess:SessionMinutes"] = "30",
            ["ClientPortal:ProviderAccess:OperatorStorePath"] = operatorStorePath
        })
        .Build();

    return ClientPortalProviderAccessOptions.FromConfiguration(configuration, workDirectory);
}

static void VerifyCloudFileSourcedSecrets(
    string workDirectory,
    List<string> checks)
{
    var receiverSecretFile = Path.Combine(workDirectory, "control-cloud-receiver-signing-secret.txt");
    var entitlementSecretFile = Path.Combine(workDirectory, "control-cloud-entitlement-signing-secret.txt");
    var appActivationPublicKeyFile = Path.Combine(workDirectory, "control-cloud-app-activation-public.pem");
    var appActivationPrivateKeyFile = Path.Combine(workDirectory, "control-cloud-app-activation-private.pem");

    var defaultAppActivationOptions = new ControlCloudAppActivationSigningOptions();
    File.WriteAllText(receiverSecretFile, "control-cloud-file-receiver-secret");
    File.WriteAllText(entitlementSecretFile, "control-cloud-file-entitlement-secret");
    File.WriteAllText(appActivationPublicKeyFile, defaultAppActivationOptions.PublicKeyPem);
    File.WriteAllText(appActivationPrivateKeyFile, defaultAppActivationOptions.PrivateKeyPem);

    var receiverOptions = new ControlCloudReceiverOptions
    {
        SigningKeys =
        [
            new ControlCloudReceiverSigningKeyOptions
            {
                KeyId = "control-desk-file-key",
                Secret = "inline-receiver-secret-should-not-be-used",
                SecretFile = receiverSecretFile
            }
        ]
    };
    receiverOptions.HydrateFileBackedSecrets(workDirectory);
    var signingKeyStore = new ConfiguredControlCloudSigningKeyStore(receiverOptions);
    Require(
        signingKeyStore.TryGetSecret("control-desk-file-key", out var receiverSecret)
        && receiverSecret == "control-cloud-file-receiver-secret",
        "Control Cloud receiver signing key should load from the configured secret file.");

    var entitlementOptions = new ControlCloudEntitlementSigningOptions
    {
        ActiveKeyId = "control-cloud-file-entitlement",
        SigningKeys =
        [
            new ControlCloudEntitlementSigningKeyOptions
            {
                KeyId = "control-cloud-file-entitlement",
                Secret = "inline-entitlement-secret-should-not-be-used",
                SecretFile = entitlementSecretFile
            }
        ]
    };
    entitlementOptions.HydrateFileBackedSecrets(workDirectory);
    Require(
        entitlementOptions.SigningKeys.Single().Secret == "control-cloud-file-entitlement-secret",
        "Control Cloud entitlement signing key should load from the configured secret file.");
    _ = new HmacControlCloudEntitlementBundleSigner(entitlementOptions);
    _ = new HmacControlCloudBootstrapPackageSigner(entitlementOptions);
    _ = new HmacControlCloudInstallationCommandSigner(entitlementOptions);

    var appActivationOptions = new ControlCloudAppActivationSigningOptions
    {
        ActiveKeyId = "control-cloud-file-app-activation",
        PublicKeyPem = "inline-public-key-should-not-be-used",
        PublicKeyPemFile = appActivationPublicKeyFile,
        PrivateKeyPem = "inline-private-key-should-not-be-used",
        PrivateKeyPemFile = appActivationPrivateKeyFile
    };
    appActivationOptions.HydrateFileBackedSecrets(workDirectory);
    var appActivationSigner = new EcdsaControlCloudAppActivationTokenSigner(appActivationOptions);
    Require(
        appActivationSigner.PublicKeyPem.Contains("BEGIN PUBLIC KEY", StringComparison.Ordinal),
        "Control Cloud app activation public key should load from the configured PEM file.");

    checks.Add("loaded Control Cloud receiver, entitlement, bootstrap, command, and app activation signing secrets from files");
}

static ClientPortalProviderAccessOptions CopyOptionsWithSessionSigningKeys(
    ClientPortalProviderAccessOptions source,
    string activeKeyId,
    ProviderAccessSessionSigningKeyOptions[] sessionSigningKeys)
{
    return new ClientPortalProviderAccessOptions
    {
        SharedSecret = source.SharedSecret,
        SharedSecretFile = source.SharedSecretFile,
        SessionSigningSecret = "",
        SessionSigningSecretFile = "",
        TotpProtectionSecret = source.TotpProtectionSecret,
        TotpProtectionSecretFile = source.TotpProtectionSecretFile,
        ActiveSessionSigningKeyId = activeKeyId,
        SessionSigningKeys = sessionSigningKeys,
        SessionMinutes = source.SessionMinutes,
        FailedLoginLockoutThreshold = source.FailedLoginLockoutThreshold,
        FailedLoginLockoutMinutes = source.FailedLoginLockoutMinutes,
        DefaultScopes = source.DefaultScopes,
        OperatorStorePath = source.OperatorStorePath,
        Users = source.Users
    };
}

static ProviderAccessAuthorizationResult AuthorizeBearerToken(
    ProviderAccessSessionService service,
    string accessToken,
    string requiredScope)
{
    var context = new DefaultHttpContext();
    context.Request.Headers.Authorization = $"Bearer {accessToken}";

    return service.Authorize(context.Request, requiredScope);
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static async Task RequireThrowsAsync<TException>(
    Func<Task> action,
    string message)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

internal sealed class FixedClock : IControlCloudClock
{
    public FixedClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; }
}

internal sealed record SmokeOptions(
    string WorkDirectory,
    bool KeepArtifacts,
    bool ShowHelp)
{
    public const string HelpText = """
        Usage:
          dotnet run --project tools/SafarSuite.ControlCloud.ProviderAccessSmoke -- [options]

        Options:
          --work-dir <path>   Directory for temporary smoke artifacts. Default: artifacts/codex/provider-access-smoke
          --keep-artifacts    Keep temporary provider operator store files.
          --help              Show help.
        """;

    public static SmokeOptions Parse(string[] args)
    {
        var workDirectory = Path.GetFullPath(Path.Combine(
            Environment.CurrentDirectory,
            "artifacts",
            "codex",
            "provider-access-smoke"));
        var keepArtifacts = false;
        var showHelp = false;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];

            switch (arg)
            {
                case "--work-dir":
                    workDirectory = Path.GetFullPath(ReadRequired(args, ref index, arg));
                    break;

                case "--keep-artifacts":
                    keepArtifacts = true;
                    break;

                case "--help":
                case "-h":
                    showHelp = true;
                    break;

                default:
                    throw new InvalidOperationException($"Unknown option '{arg}'.{Environment.NewLine}{HelpText}");
            }
        }

        return new SmokeOptions(workDirectory, keepArtifacts, showHelp);
    }

    private static string ReadRequired(string[] args, ref int index, string option)
    {
        if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new InvalidOperationException($"{option} requires a value.");
        }

        return args[index].Trim();
    }
}
