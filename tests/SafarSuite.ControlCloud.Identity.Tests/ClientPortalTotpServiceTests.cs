using OtpNet;
using SafarSuite.ControlCloud.Infrastructure.ClientPortal;

namespace SafarSuite.ControlCloud.Identity.Tests;

public sealed class ClientPortalTotpServiceTests
{
    [Fact]
    public void FirstCodeIsAcceptedAndTheSameTimestepCannotBeReplayed()
    {
        var service = new OtpNetClientPortalTotpService();
        var now = new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);
        var secret = service.CreateSecret();
        var code = new Totp(
                Base32Encoding.ToBytes(secret),
                step: 30,
                mode: OtpHashMode.Sha1,
                totpSize: 6)
            .ComputeTotp(now.UtcDateTime);

        var firstAccepted = service.TryVerifyCode(
            secret,
            code,
            now,
            lastAcceptedStep: null,
            out var acceptedStep);
        var replayAccepted = service.TryVerifyCode(
            secret,
            code,
            now,
            acceptedStep,
            out _);

        Assert.True(firstAccepted);
        Assert.Equal(now.ToUnixTimeSeconds() / 30, acceptedStep);
        Assert.False(replayAccepted);
    }

    [Fact]
    public void EnrollmentUriCanBeRenderedAsSvgAndDataUri()
    {
        var service = new OtpNetClientPortalTotpService();
        var uri = service.CreateOtpAuthUri(
            "SafarSuite Client Portal",
            "billing@example.test",
            service.CreateSecret());

        var svg = service.CreateQrCodeSvg(uri);
        var dataUri = service.CreateQrCodeDataUri(uri);

        Assert.StartsWith("otpauth://totp/", uri, StringComparison.Ordinal);
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("data:image/svg+xml;base64,", dataUri, StringComparison.Ordinal);
    }
}
