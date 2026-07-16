namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record GetClientPortalCommercialDocumentsQuery(
    Guid ClientId,
    string DocumentType,
    int Take,
    string? Cursor);
