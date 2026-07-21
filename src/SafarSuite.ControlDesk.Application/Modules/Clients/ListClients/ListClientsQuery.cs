namespace SafarSuite.ControlDesk.Application.Modules.Clients.ListClients;

public sealed record ListClientsQuery(
    string? Search = null,
    string? Status = null,
    string? Sort = null,
    string? Direction = null,
    int Take = 50,
    string? Cursor = null);
