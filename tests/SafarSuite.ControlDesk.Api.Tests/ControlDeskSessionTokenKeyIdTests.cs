using System.Text;
using System.Text.Json;
using SafarSuite.ControlDesk.Api.Modules.Auth;
using SafarSuite.ControlDesk.Application.Modules.Auth.AuthenticateLocalOperator;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class ControlDeskSessionTokenKeyIdTests
{
    [Fact]
    public void Issued_session_contains_the_active_signing_key_identifier()
    {
        var provider = new TestSigningKeyProvider("machine-generation-123");
        var clock = new MutableTimeProvider(
            new DateTimeOffset(2026, 7, 20, 2, 0, 0, TimeSpan.Zero));
        var service = new ControlDeskSessionTokenService(provider, clock);
        var principal = new LocalOperatorSessionPrincipal(
            Guid.Parse("0d6ce9d0-5bd2-4d96-9d11-5b5d52fb4d50"),
            "admin@example.test",
            "Administrator",
            ["Administrator"],
            ["control-desk:admin"],
            1);

        var issued = service.Issue(principal, 5);
        var payload = issued.AccessToken[..issued.AccessToken.IndexOf('.')];
        var json = JsonDocument.Parse(
            Encoding.UTF8.GetString(DecodeBase64Url(payload)));

        Assert.Equal("machine-generation-123", json.RootElement.GetProperty("signingKeyId").GetString());
        Assert.True(service.Validate(issued.AccessToken).IsValid);
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }

    private sealed class TestSigningKeyProvider(string keyId) : IControlDeskSessionSigningKeyProvider
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes(
            "test-machine-signing-key-material-at-least-32-bytes");

        public string SessionSigningKeyId => keyId;

        public byte[] CopySessionSigningKey() => Key.ToArray();
    }
}
