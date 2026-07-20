using System.Security.Principal;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlDesk.Application.Modules.Auth.ProvisionFirstOperator;
using SafarSuite.ControlDesk.Domain.Modules.Auth;
using SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;
using SafarSuite.ControlDesk.Infrastructure.Security;

if (args is ["--help"] or ["-h"])
{
    Console.WriteLine("SafarSuite Control Desk first-operator bootstrap");
    Console.WriteLine("Usage: dotnet run --project tools/SafarSuite.ControlDesk.FirstOperator -- --connection <PostgreSQL connection string>");
    return 0;
}

if (!OperatingSystem.IsWindows()
    || !new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
{
    Console.Error.WriteLine("First-operator provisioning requires an elevated Windows administrator shell.");
    return 2;
}

var connection = ReadOption("--connection")
    ?? Environment.GetEnvironmentVariable("ControlDesk__ConnectionStrings__ControlDesk");
if (string.IsNullOrWhiteSpace(connection))
{
    Console.Error.WriteLine("A PostgreSQL connection string is required; it was not written to output.");
    return 2;
}

var email = ReadRequired("Operator email: ");
var fullName = ReadRequired("Operator full name: ");
var password = ReadSecret("Operator password: ");
var confirmation = ReadSecret("Confirm operator password: ");
if (!string.Equals(password, confirmation, StringComparison.Ordinal))
{
    Console.Error.WriteLine("Passwords did not match.");
    return 2;
}

await using var db = new ControlDeskDbContext(
    new DbContextOptionsBuilder<ControlDeskDbContext>()
        .UseNpgsql(connection)
        .Options);

var decision = FirstOperatorProvisioningPolicy.Evaluate(new(
    IsElevated: true,
    OperatorAlreadyExists: await db.LocalOperators.AnyAsync(),
    email,
    fullName,
    password));
if (!decision.IsAllowed)
{
    Console.Error.WriteLine(decision.ErrorMessage);
    return 2;
}

var codec = new Pbkdf2LocalOperatorPasswordCodec();
var localOperator = LocalOperator.CreateFirstAdministrator(
    LocalOperatorId.Create(Guid.NewGuid()),
    LocalOperatorEmail.Create(email),
    fullName,
    codec.Hash(password),
    DateTimeOffset.UtcNow);

await using var transaction = await db.Database.BeginTransactionAsync();
db.LocalOperators.Add(localOperator);
await db.SaveChangesAsync();
await transaction.CommitAsync();
Console.WriteLine("First operator provisioned. Credentials were not echoed or logged.");
return 0;

string? ReadOption(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static string ReadRequired(string prompt)
{
    Console.Write(prompt);
    var value = Console.ReadLine()?.Trim() ?? string.Empty;
    if (value.Length == 0) throw new InvalidOperationException("Required input was empty.");
    return value;
}

static string ReadSecret(string prompt)
{
    Console.Write(prompt);
    var chars = new List<char>();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) break;
        if (key.Key == ConsoleKey.Backspace && chars.Count > 0) { chars.RemoveAt(chars.Count - 1); continue; }
        if (!char.IsControl(key.KeyChar)) chars.Add(key.KeyChar);
    }
    Console.WriteLine();
    return new string(chars.ToArray());
}
