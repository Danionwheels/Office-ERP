using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using OtpNet;
using QRCoder;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Infrastructure.ClientPortal;

public sealed class OtpNetClientPortalTotpService : IClientPortalTotpService
{
    private const int SecretByteCount = 20;
    private const int Digits = 6;
    private const int PeriodSeconds = 30;
    private const int VerificationWindowSteps = 1;

    public string CreateSecret()
    {
        return Base32Encoding.ToString(
            RandomNumberGenerator.GetBytes(SecretByteCount));
    }

    public string CreateOtpAuthUri(
        string issuer,
        string accountName,
        string secret)
    {
        var normalizedIssuer = string.IsNullOrWhiteSpace(issuer)
            ? "SafarSuite Client Portal"
            : issuer.Trim();
        var normalizedAccount = string.IsNullOrWhiteSpace(accountName)
            ? "client-portal-user"
            : accountName.Trim();
        var label = $"{normalizedIssuer}:{normalizedAccount}";

        return $"otpauth://totp/{Uri.EscapeDataString(label)}"
            + $"?secret={Uri.EscapeDataString(NormalizeSecret(secret))}"
            + $"&issuer={Uri.EscapeDataString(normalizedIssuer)}"
            + $"&algorithm=SHA1&digits={Digits.ToString(CultureInfo.InvariantCulture)}"
            + $"&period={PeriodSeconds.ToString(CultureInfo.InvariantCulture)}";
    }

    public string CreateQrCodeSvg(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("QR code value is required.", nameof(value));
        }

        using var qrCodeData = QRCodeGenerator.GenerateQrCode(
            value.Trim(),
            QRCodeGenerator.ECCLevel.Q);
        var qrCode = new SvgQRCode(qrCodeData);

        return qrCode.GetGraphic(5);
    }

    public string CreateQrCodeDataUri(string value)
    {
        var svg = CreateQrCodeSvg(value);
        var encodedSvg = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));

        return $"data:image/svg+xml;base64,{encodedSvg}";
    }

    public bool TryVerifyCode(
        string secret,
        string? code,
        DateTimeOffset now,
        long? lastAcceptedStep,
        out long acceptedStep)
    {
        acceptedStep = 0;
        var normalizedCode = NormalizeCode(code);

        if (normalizedCode.Length != Digits
            || normalizedCode.Any(character => !char.IsDigit(character))
            || !TryCreateTotp(secret, out var totp))
        {
            return false;
        }

        var currentStep = GetStep(now);

        for (var offset = -VerificationWindowSteps; offset <= VerificationWindowSteps; offset++)
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
            var expectedCode = totp!.ComputeTotp(candidateTime);

            if (!FixedTimeEquals(normalizedCode, expectedCode))
            {
                continue;
            }

            acceptedStep = candidateStep;
            return true;
        }

        return false;
    }

    private static bool TryCreateTotp(
        string secret,
        out Totp? totp)
    {
        totp = null;
        var normalizedSecret = NormalizeSecret(secret);

        if (string.IsNullOrWhiteSpace(normalizedSecret))
        {
            return false;
        }

        try
        {
            var secretBytes = Base32Encoding.ToBytes(normalizedSecret);

            if (secretBytes.Length == 0)
            {
                return false;
            }

            totp = new Totp(
                secretBytes,
                step: PeriodSeconds,
                mode: OtpHashMode.Sha1,
                totpSize: Digits);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static long GetStep(DateTimeOffset now)
    {
        return now.ToUnixTimeSeconds() / PeriodSeconds;
    }

    private static string NormalizeSecret(string? secret)
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
        var leftBytes = Encoding.ASCII.GetBytes(left);
        var rightBytes = Encoding.ASCII.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
