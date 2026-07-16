using SafarSuite.ControlCloud.Application.Modules.ClientPortal.UploadPortalAttachment;

namespace SafarSuite.ControlCloud.Identity.Tests;

public sealed class ClientPortalAttachmentContentValidatorTests
{
    private readonly ClientPortalAttachmentContentValidator _validator = new();

    [Fact]
    public void Validates_jpeg_extension_content_type_and_signature_together()
    {
        var result = _validator.Validate(
            "transfer-proof.jpeg",
            "image/jpeg",
            [0xFF, 0xD8, 0xFF, 0xE0, 0x00]);

        Assert.True(result.IsSuccess);
        Assert.Equal("image/jpeg", result.DetectedContentType);
    }

    [Fact]
    public void Rejects_declared_pdf_with_a_non_pdf_signature()
    {
        var result = _validator.Validate(
            "transfer-proof.pdf",
            "application/pdf",
            [0x89, 0x50, 0x4E, 0x47]);

        Assert.False(result.IsSuccess);
        Assert.Equal("PaymentProofTypeInvalid", result.FailureCode);
    }

    [Fact]
    public void Rejects_a_valid_signature_when_the_extension_does_not_match()
    {
        var result = _validator.Validate(
            "transfer-proof.png",
            "image/png",
            [0x25, 0x50, 0x44, 0x46, 0x2D]);

        Assert.False(result.IsSuccess);
        Assert.Equal("PaymentProofTypeInvalid", result.FailureCode);
    }
}
