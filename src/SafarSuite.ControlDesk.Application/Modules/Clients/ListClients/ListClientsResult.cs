namespace SafarSuite.ControlDesk.Application.Modules.Clients.ListClients;

public sealed record ListClientsResult(
    IReadOnlyCollection<ClientLookupResult> Clients);

public sealed record ClientLookupResult(
    Guid ClientId,
    string Code,
    string LegalName,
    string DisplayName,
    string Status);
