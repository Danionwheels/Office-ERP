namespace SafarSuite.ControlDesk.Application.Modules.Clients.CreateClient;

public sealed record CreateClientResult(
    Guid ClientId,
    string Code,
    string LegalName,
    string DisplayName,
    string Status);
