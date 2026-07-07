using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Api.Modules.ClientPortal;
using SafarSuite.ControlCloud.Api.Modules.ProviderAccess;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Infrastructure.ClientPortal;
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
        fileStore);
    var session = await sessionService.CreateSessionFromCredentialsAsync(
        "seed.provider@safarsuite.local",
        "SeedProviderPass123!",
        [ProviderAccessScopes.ProviderOperatorsManage],
        expiresInMinutes: 10);

    Require(session.IsSuccess, "valid provider operator credentials should issue a session.");
    Require(session.Scopes?.Single() == ProviderAccessScopes.ProviderOperatorsManage, "session should carry requested scope.");
    checks.Add("issued scoped provider operator session");

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
        CreatedAtUtc = source.CreatedAtUtc,
        CreatedBy = source.CreatedBy,
        UpdatedAtUtc = source.UpdatedAtUtc,
        UpdatedBy = source.UpdatedBy,
        LastLoginAtUtc = source.LastLoginAtUtc
    };
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
