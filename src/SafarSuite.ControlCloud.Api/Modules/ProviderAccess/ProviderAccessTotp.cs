using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public static class ProviderAccessTotp
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int SecretByteCount = 20;
    private const int Digits = 6;
    private const int PeriodSeconds = 30;
    private const int VerificationWindow = 1;

    public static string CreateSecret()
    {
        Span<byte> secretBytes = stackalloc byte[SecretByteCount];
        RandomNumberGenerator.Fill(secretBytes);

        return Base32Encode(secretBytes);
    }

    public static string CreateOtpAuthUri(
        string issuer,
        string accountName,
        string secret)
    {
        var normalizedIssuer = string.IsNullOrWhiteSpace(issuer)
            ? "SafarSuite Provider"
            : issuer.Trim();
        var normalizedAccount = string.IsNullOrWhiteSpace(accountName)
            ? "provider-operator"
            : accountName.Trim();
        var label = $"{normalizedIssuer}:{normalizedAccount}";

        return $"otpauth://totp/{Uri.EscapeDataString(label)}"
            + $"?secret={Uri.EscapeDataString(NormalizeSecret(secret))}"
            + $"&issuer={Uri.EscapeDataString(normalizedIssuer)}"
            + $"&algorithm=SHA1&digits={Digits.ToString(CultureInfo.InvariantCulture)}"
            + $"&period={PeriodSeconds.ToString(CultureInfo.InvariantCulture)}";
    }

    public static string CreateCode(
        string secret,
        DateTimeOffset now)
    {
        return ComputeCode(DecodeBase32(secret), GetStep(now))
            .ToString($"D{Digits}", CultureInfo.InvariantCulture);
    }

    public static bool TryVerifyCode(
        string secret,
        string? code,
        DateTimeOffset now,
        long? lastAcceptedStep,
        out long acceptedStep)
    {
        acceptedStep = 0;

        var normalizedCode = NormalizeCode(code);

        if (normalizedCode.Length != Digits
            || normalizedCode.Any(character => !char.IsDigit(character)))
        {
            return false;
        }

        byte[] secretBytes;

        try
        {
            secretBytes = DecodeBase32(secret);
        }
        catch (FormatException)
        {
            return false;
        }

        var currentStep = GetStep(now);

        for (var offset = -VerificationWindow; offset <= VerificationWindow; offset++)
        {
            var candidateStep = currentStep + offset;

            if (candidateStep < 0
                || (lastAcceptedStep is not null && candidateStep <= lastAcceptedStep.Value))
            {
                continue;
            }

            var expectedCode = ComputeCode(secretBytes, candidateStep)
                .ToString($"D{Digits}", CultureInfo.InvariantCulture);

            if (!FixedTimeEquals(normalizedCode, expectedCode))
            {
                continue;
            }

            acceptedStep = candidateStep;
            return true;
        }

        return false;
    }

    private static long GetStep(DateTimeOffset now)
    {
        return now.ToUnixTimeSeconds() / PeriodSeconds;
    }

    private static int ComputeCode(
        byte[] secretBytes,
        long step)
    {
        var counter = BitConverter.GetBytes(step);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counter);
        }

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0x0f;
        var binaryCode =
            ((hash[offset] & 0x7f) << 24)
            | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8)
            | (hash[offset + 3] & 0xff);

        return binaryCode % (int)Math.Pow(10, Digits);
    }

    private static string NormalizeSecret(string secret)
    {
        return new string((secret ?? "")
            .Where(character => !char.IsWhiteSpace(character) && character is not '-' and not '=')
            .Select(character => char.ToUpperInvariant(character))
            .ToArray());
    }

    private static string NormalizeCode(string? code)
    {
        return new string((code ?? "")
            .Where(character => !char.IsWhiteSpace(character) && character != '-')
            .ToArray());
    }

    private static string Base32Encode(ReadOnlySpan<byte> data)
    {
        var output = new StringBuilder((data.Length + 4) / 5 * 8);
        var buffer = (int)data[0];
        var next = 1;
        var bitsLeft = 8;

        while (bitsLeft > 0 || next < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    buffer <<= 8;
                    buffer |= data[next++] & 0xff;
                    bitsLeft += 8;
                }
                else
                {
                    var padding = 5 - bitsLeft;
                    buffer <<= padding;
                    bitsLeft += padding;
                }
            }

            var index = (buffer >> (bitsLeft - 5)) & 0x1f;
            bitsLeft -= 5;
            output.Append(Base32Alphabet[index]);
        }

        return output.ToString();
    }

    private static byte[] DecodeBase32(string secret)
    {
        var normalizedSecret = NormalizeSecret(secret);

        if (string.IsNullOrWhiteSpace(normalizedSecret))
        {
            throw new FormatException("TOTP secret is required.");
        }

        var bytes = new List<byte>();
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var character in normalizedSecret)
        {
            var value = Base32Alphabet.IndexOf(character);

            if (value < 0)
            {
                throw new FormatException("TOTP secret is not base32 encoded.");
            }

            buffer = (buffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft < 8)
            {
                continue;
            }

            bytes.Add((byte)((buffer >> (bitsLeft - 8)) & 0xff));
            bitsLeft -= 8;
        }

        return bytes.ToArray();
    }

    private static bool FixedTimeEquals(
        string left,
        string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var leftBytes = Encoding.ASCII.GetBytes(left);
        var rightBytes = Encoding.ASCII.GetBytes(right);

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
