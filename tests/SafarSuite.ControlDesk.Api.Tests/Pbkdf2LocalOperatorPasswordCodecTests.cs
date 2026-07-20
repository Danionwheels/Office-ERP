using SafarSuite.ControlDesk.Infrastructure.Security;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class Pbkdf2LocalOperatorPasswordCodecTests
{
    private readonly Pbkdf2LocalOperatorPasswordCodec _codec = new();

    [Fact]
    public void Verify_accepts_the_reviewed_golden_vector()
    {
        Assert.True(_codec.Verify(GoldenPassword, GoldenHash));
        Assert.False(_codec.Verify("not-the-password", GoldenHash));
    }

    [Fact]
    public void Hash_produces_the_accepted_canonical_shape_and_round_trips()
    {
        var encoded = _codec.Hash("New-operator-password-123!");
        var parts = encoded.Split('.');

        Assert.Equal(4, parts.Length);
        Assert.Equal("pbkdf2-sha256", parts[0]);
        Assert.Equal("120000", parts[1]);
        Assert.DoesNotContain('=', parts[2]);
        Assert.DoesNotContain('=', parts[3]);
        Assert.True(_codec.Verify("New-operator-password-123!", encoded));
    }

    [Fact]
    public void Verify_rejects_a_tampered_hash()
    {
        var replacement = GoldenHash.EndsWith('A') ? 'B' : 'A';
        var tampered = GoldenHash[..^1] + replacement;

        Assert.False(_codec.Verify(GoldenPassword, tampered));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("pbkdf2-sha256")]
    [InlineData("pbkdf2-sha256.0.AAECAwQFBgcICQoLDA0ODw.GRxYGMDYtjN-kbO2SA72GEAvAeXewYCXAAkTWoXMTxk")]
    [InlineData("pbkdf2-sha256.1.AAECAwQFBgcICQoLDA0ODw.GRxYGMDYtjN-kbO2SA72GEAvAeXewYCXAAkTWoXMTxk")]
    [InlineData("pbkdf2-sha256.1000001.AAECAwQFBgcICQoLDA0ODw.GRxYGMDYtjN-kbO2SA72GEAvAeXewYCXAAkTWoXMTxk")]
    [InlineData("pbkdf2-sha256.0120000.AAECAwQFBgcICQoLDA0ODw.GRxYGMDYtjN-kbO2SA72GEAvAeXewYCXAAkTWoXMTxk")]
    [InlineData("pbkdf2-sha1.120000.AAECAwQFBgcICQoLDA0ODw.GRxYGMDYtjN-kbO2SA72GEAvAeXewYCXAAkTWoXMTxk")]
    [InlineData("pbkdf2-sha256.120000.not*base64.GRxYGMDYtjN-kbO2SA72GEAvAeXewYCXAAkTWoXMTxk")]
    [InlineData("pbkdf2-sha256.120000.AAECAwQ.GRxYGMDYtjN-kbO2SA72GEAvAeXewYCXAAkTWoXMTxk")]
    [InlineData("pbkdf2-sha256.120000.AAECAwQFBgcICQoLDA0ODw.")]
    public void Verify_rejects_malformed_or_unsupported_values(string? passwordHash)
    {
        Assert.False(_codec.Verify(GoldenPassword, passwordHash));
    }

    private const string GoldenPassword = "Golden-password-123!";

    private const string GoldenHash =
        "pbkdf2-sha256.120000.AAECAwQFBgcICQoLDA0ODw.GRxYGMDYtjN-kbO2SA72GEAvAeXewYCXAAkTWoXMTxk";
}
