namespace SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

public sealed class ControlCloudClientPortalAttachment
{
    public Guid AttachmentId { get; set; }
    public Guid ClientId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = "";
    public byte[] Content { get; set; } = [];
    public DateTimeOffset UploadedAtUtc { get; set; }

    public static ControlCloudClientPortalAttachment Create(
        Guid attachmentId,
        Guid clientId,
        Guid uploadedByUserId,
        string fileName,
        string contentType,
        byte[] content,
        string sha256,
        DateTimeOffset uploadedAtUtc)
    {
        if (attachmentId == Guid.Empty || clientId == Guid.Empty || uploadedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Attachment identifiers are required.");
        }

        if (content.Length is <= 0 or > 5 * 1024 * 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(content), "Attachment must be between 1 byte and 5 MB.");
        }

        return new ControlCloudClientPortalAttachment
        {
            AttachmentId = attachmentId,
            ClientId = clientId,
            UploadedByUserId = uploadedByUserId,
            FileName = CleanFileName(fileName),
            ContentType = contentType.Trim().ToLowerInvariant(),
            SizeBytes = content.LongLength,
            Sha256 = sha256.Trim().ToLowerInvariant(),
            Content = content,
            UploadedAtUtc = uploadedAtUtc
        };
    }

    private static string CleanFileName(string value)
    {
        var clean = Path.GetFileName((value?.Trim() ?? "").Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(clean))
        {
            throw new ArgumentException("Attachment file name is required.", nameof(value));
        }

        return clean.Length <= 255 ? clean : clean[..255];
    }
}
