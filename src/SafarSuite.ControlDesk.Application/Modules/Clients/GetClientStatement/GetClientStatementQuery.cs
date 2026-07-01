namespace SafarSuite.ControlDesk.Application.Modules.Clients.GetClientStatement;

public sealed record GetClientStatementQuery(
    Guid ClientId,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null);
