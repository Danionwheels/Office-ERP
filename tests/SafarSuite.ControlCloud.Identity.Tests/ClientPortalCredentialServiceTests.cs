using SafarSuite.ControlCloud.Infrastructure.ClientPortal;

namespace SafarSuite.ControlCloud.Identity.Tests;

public sealed class ClientPortalCredentialServiceTests
{
    [Fact]
    public void SecureTokensDecodeToAtLeastThirtyTwoRandomBytes()
    {
        var service = new HmacClientPortalCredentialService(new ClientPortalAccessOptions
        {
            SessionSigningSecret = "identity-unit-test-signing-secret"
        });

        var firstToken = service.CreateSecureToken(byteCount: 1);
        var secondToken = service.CreateSecureToken(byteCount: 32);

        Assert.True(DecodeBase64Url(firstToken).Length >= 32);
        Assert.True(DecodeBase64Url(secondToken).Length >= 32);
        Assert.NotEqual(firstToken, secondToken);
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var base64 = value
            .Replace('-', '+')
            .Replace('_', '/');
        var padding = base64.Length % 4;

        if (padding > 0)
        {
            base64 = base64.PadRight(base64.Length + 4 - padding, '=');
        }

        return Convert.FromBase64String(base64);
    }
}
