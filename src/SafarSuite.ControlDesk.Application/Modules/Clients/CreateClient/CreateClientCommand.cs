namespace SafarSuite.ControlDesk.Application.Modules.Clients.CreateClient;

public sealed record CreateClientCommand(
    string Code,
    string LegalName,
    string? DisplayName);
