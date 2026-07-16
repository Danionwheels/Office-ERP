using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using OtpNet;

namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public static class ProviderAccessTotp
{
    private const int SecretByteCount = 20;
    private const int Digits = 6;
    private const int PeriodSeconds = 30;
    private const int VerificationWindow = 1;

    public static string CreateSecret()
    {
        return Base32Encoding.ToString(
            RandomNumberGenerator.GetBytes(SecretByteCount));
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
        return CreateTotp(secret).ComputeTotp(now.UtcDateTime);
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

        Totp totp;

        try
        {
            totp = CreateTotp(secret);
        }
        catch (ArgumentException)
        {
            return false;
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

            var candidateTime = DateTimeOffset
                .FromUnixTimeSeconds(candidateStep * PeriodSeconds)
                .UtcDateTime;
            var expectedCode = totp.ComputeTotp(candidateTime);

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

    private static Totp CreateTotp(string secret)
    {
        var normalizedSecret = NormalizeSecret(secret);

        if (string.IsNullOrWhiteSpace(normalizedSecret))
        {
            throw new FormatException("TOTP secret is required.");
        }

        var secretBytes = Base32Encoding.ToBytes(normalizedSecret);

        if (secretBytes.Length == 0)
        {
            throw new FormatException("TOTP secret is required.");
        }

        return new Totp(
            secretBytes,
            step: PeriodSeconds,
            mode: OtpHashMode.Sha1,
            totpSize: Digits);
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
