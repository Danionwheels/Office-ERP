namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.UploadPortalAttachment;

public sealed record UploadClientPortalAttachmentCommand(
    Guid ClientId,
    Guid UploadedByUserId,
    string FileName,
    string ContentType,
    byte[] Content);
