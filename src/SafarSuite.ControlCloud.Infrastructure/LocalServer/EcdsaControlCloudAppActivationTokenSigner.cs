using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class EcdsaControlCloudAppActivationTokenSigner
    : IControlCloudAppActivationTokenSigner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _privateKeyPem;

    public EcdsaControlCloudAppActivationTokenSigner(
        ControlCloudAppActivationSigningOptions options)
    {
        SigningKeyId = options.ActiveKeyId.Trim();
        PublicKeyPem = options.PublicKeyPem.Trim();
        _privateKeyPem = options.PrivateKeyPem.Trim();

        if (string.IsNullOrWhiteSpace(SigningKeyId))
        {
            throw new InvalidOperationException(
                "ControlCloud:AppActivationSigning:ActiveKeyId is required.");
        }

        if (string.IsNullOrWhiteSpace(PublicKeyPem))
        {
            throw new InvalidOperationException(
                "ControlCloud:AppActivationSigning:PublicKeyPem is required.");
        }

        if (string.IsNullOrWhiteSpace(_privateKeyPem))
        {
            throw new InvalidOperationException(
                "ControlCloud:AppActivationSigning:PrivateKeyPem is required.");
        }
    }

    public string SigningKeyId { get; }

    public string PublicKeyPem { get; }

    public SafarSuiteAppActivationSignedToken Sign(
        SafarSuiteAppActivationTokenClaims claims)
    {
        var tokenBytes = JsonSerializer.SerializeToUtf8Bytes(claims, JsonOptions);
        var activationToken = Base64UrlEncode(tokenBytes);

        using var signer = ECDsa.Create();
        signer.ImportFromPem(_privateKeyPem);
        var signature = signer.SignData(
            Encoding.UTF8.GetBytes(activationToken),
            HashAlgorithmName.SHA256);

        return new SafarSuiteAppActivationSignedToken(
            activationToken,
            Base64UrlEncode(signature));
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
