namespace SafarSuite.ControlDesk.Application.Modules.Clients.UpdateClient;

public sealed record UpdateClientCommand(
    Guid ClientId,
    string LegalName,
    string? DisplayName);
