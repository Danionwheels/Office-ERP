using System.Security.Cryptography;
using System.Text;

namespace SafarSuite.StagingPreflight;

internal static class CryptographicMaterialValidator
{
    private const string P256ObjectIdentifier = "1.2.840.10045.3.1.7";

    public static bool HasMinimumUtf8Bytes(string value, int minimumBytes) =>
        Encoding.UTF8.GetByteCount(value) >= minimumBytes;

    public static bool HasMinimumDistinctCharacters(string value, int minimumCharacters) =>
        value.Distinct().Take(minimumCharacters).Count() >= minimumCharacters;

    public static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        try
        {
            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(leftBytes);
            CryptographicOperations.ZeroMemory(rightBytes);
        }
    }

    public static bool IsMatchingP256KeyPair(string publicPem, string privatePem)
    {
        if (publicPem.Contains("PRIVATE KEY", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            using var privateKey = ECDsa.Create();
            privateKey.ImportFromPem(privatePem);
            var privateParameters = privateKey.ExportParameters(true);

            try
            {
                using var publicKey = ECDsa.Create();
                publicKey.ImportFromPem(publicPem);
                var publicParameters = publicKey.ExportParameters(false);

                if (!IsP256(privateParameters)
                    || !IsP256(publicParameters)
                    || privateParameters.D is not { Length: 32 }
                    || privateParameters.Q.X is not { Length: 32 }
                    || privateParameters.Q.Y is not { Length: 32 }
                    || publicParameters.Q.X is not { Length: 32 }
                    || publicParameters.Q.Y is not { Length: 32 })
                {
                    return false;
                }

                var proof = "SafarSuite staging preflight key-pair proof"u8.ToArray();
                var signature = privateKey.SignData(proof, HashAlgorithmName.SHA256);

                try
                {
                    return publicKey.VerifyData(proof, signature, HashAlgorithmName.SHA256);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(signature);
                }
            }
            finally
            {
                if (privateParameters.D is not null)
                {
                    CryptographicOperations.ZeroMemory(privateParameters.D);
                }
            }
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsP256(ECParameters parameters) =>
        string.Equals(parameters.Curve.Oid.Value, P256ObjectIdentifier, StringComparison.Ordinal)
        || string.Equals(parameters.Curve.Oid.FriendlyName, "nistP256", StringComparison.OrdinalIgnoreCase)
        || string.Equals(parameters.Curve.Oid.FriendlyName, "ECDSA_P256", StringComparison.OrdinalIgnoreCase);

}
