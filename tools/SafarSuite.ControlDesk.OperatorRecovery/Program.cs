using System.Security.Principal;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;
using SafarSuite.ControlDesk.Infrastructure.Security;
using SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

if (args is ["--help"] or ["-h"])
{
    Console.WriteLine("SafarSuite Control Desk offline operator recovery");
    Console.WriteLine("Usage: dotnet run --project tools/SafarSuite.ControlDesk.OperatorRecovery -- --connection <PostgreSQL connection string> --actor <actor> --reason <reason> [--reissue-machine-secret]");
    return 0;
}

if (!OperatingSystem.IsWindows()
    || !new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
{
    Console.Error.WriteLine("Operator recovery requires an elevated Windows administrator shell.");
    return 2;
}

var connection = Option("--connection")
    ?? Environment.GetEnvironmentVariable("ControlDesk__ConnectionStrings__ControlDesk");
var actor = Option("--actor");
var reason = Option("--reason");
if (string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(actor) || string.IsNullOrWhiteSpace(reason))
{
    Console.Error.WriteLine("Connection, actor, and reason are required; secrets are never printed.");
    return 2;
}

Console.Write("Target operator email: ");
var email = Console.ReadLine()?.Trim() ?? string.Empty;
var password = ReadSecret("New operator password: ");
var confirmation = ReadSecret("Confirm new operator password: ");
if (!string.Equals(password, confirmation, StringComparison.Ordinal))
{
    Console.Error.WriteLine("Passwords did not match.");
    return 2;
}

await using var db = new ControlDeskDbContext(
    new DbContextOptionsBuilder<ControlDeskDbContext>().UseNpgsql(connection).Options);
var target = await db.LocalOperators.SingleOrDefaultAsync(
    candidate => candidate.NormalizedEmail == email.ToUpperInvariant());
if (target is null)
{
    Console.Error.WriteLine("The target operator was not found.");
    return 2;
}

var oldVersion = target.SecurityVersion;
target.ChangePasswordHash(new Pbkdf2LocalOperatorPasswordCodec().Hash(password), DateTimeOffset.UtcNow);
await db.SaveChangesAsync();

if (!args.Contains("--reissue-machine-secret", StringComparer.Ordinal))
{
    Console.WriteLine($"Operator password recovered; sessions invalidated from security version {oldVersion} to {target.SecurityVersion}.");
    return 0;
}

var store = new ControlDeskMachineSecretEnvelopeStore(
    ControlDeskMachineSecretPaths.GetCanonicalEnvelopePath(),
    ControlDeskMachineSecretAccessProfile.PreService);
var reissue = new ControlDeskMachineSecretReissueService(store).Reissue(actor, reason);
Console.WriteLine($"Operator recovery complete; sessions invalidated from security version {oldVersion} to {target.SecurityVersion}; machine-secret generation {reissue.GenerationId:D} is active. Restart the API service to load the new signing key.");
return 0;

string? Option(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static string ReadSecret(string prompt)
{
    Console.Write(prompt);
    var chars = new List<char>();
    while (true)
    {
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Enter) break;
        if (key.Key == ConsoleKey.Backspace && chars.Count > 0) { chars.RemoveAt(chars.Count - 1); continue; }
        if (!char.IsControl(key.KeyChar)) chars.Add(key.KeyChar);
    }
    Console.WriteLine();
    return new string(chars.ToArray());
}
