namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.UploadPortalAttachment;

public sealed class ClientPortalAttachmentContentValidator
{
    public const int MaximumSizeBytes = 5 * 1024 * 1024;

    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly byte[] PdfSignature = [37, 80, 68, 70, 45];

    public ClientPortalAttachmentValidationResult Validate(
        string fileName,
        string contentType,
        byte[] content)
    {
        if (content.Length is <= 0 or > MaximumSizeBytes)
        {
            return Failure("PaymentProofSizeInvalid", "Payment proof must be between 1 byte and 5 MB.");
        }

        var extension = Path.GetExtension(Path.GetFileName(fileName)).ToLowerInvariant();
        var detectedType = DetectContentType(content);
        if (detectedType is null)
        {
            return Failure("PaymentProofTypeInvalid", "Payment proof must be a JPEG, PNG, or PDF file.");
        }

        var extensionMatches = detectedType switch
        {
            "image/jpeg" => extension is ".jpg" or ".jpeg",
            "image/png" => extension == ".png",
            "application/pdf" => extension == ".pdf",
            _ => false
        };
        var normalizedDeclaredType = contentType.Trim().ToLowerInvariant();
        var declaredTypeMatches = normalizedDeclaredType.Length == 0
            || normalizedDeclaredType == "application/octet-stream"
            || normalizedDeclaredType == detectedType;

        return extensionMatches && declaredTypeMatches
            ? new ClientPortalAttachmentValidationResult(true, detectedType, null, null)
            : Failure("PaymentProofTypeInvalid", "Payment proof extension, content type, and file signature must match.");
    }

    private static string? DetectContentType(ReadOnlySpan<byte> content)
    {
        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (content.StartsWith(PngSignature))
        {
            return "image/png";
        }

        return content.StartsWith(PdfSignature) ? "application/pdf" : null;
    }

    private static ClientPortalAttachmentValidationResult Failure(string code, string detail) =>
        new(false, null, code, detail);
}

public sealed record ClientPortalAttachmentValidationResult(
    bool IsSuccess,
    string? DetectedContentType,
    string? FailureCode,
    string? Detail);
