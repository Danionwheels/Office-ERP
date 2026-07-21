namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudOutboxMessages;

public sealed record ListCloudOutboxMessagesQuery(
    string? Status,
    string? MessageType,
    Guid? ClientId,
    int Take,
    string? Cursor);
