using SafarSuite.ControlCloud.Infrastructure;

namespace SafarSuite.ControlCloud.Infrastructure.LocalServer;

public sealed class ControlCloudAppActivationSigningOptions
{
    public const string SectionName = "ControlCloud:AppActivationSigning";

    public string ActiveKeyId { get; set; } = "compose-proof-ecdsa-p256-2026-07";

    public string IssueStorePath { get; set; } = "App_Data/control-cloud-app-activation-issues.json";

    public string PublicKeyPem { get; set; } =
        """
        -----BEGIN PUBLIC KEY-----
        MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEplJxLk27PAx9Jh5gFKq/cL5e9V67
        GFv6MGWoiHl8PPfJtIpnA2gFoxQtAR4/QvnjJ4JvzcxIqkuW23fHR9pQUg==
        -----END PUBLIC KEY-----
        """;

    public string PublicKeyPemFile { get; set; } = "";

    public string PrivateKeyPem { get; set; } =
        """
        -----BEGIN PRIVATE KEY-----
        MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQguqVBr4zkZCln6vvf
        UBnrIJDBrtx8TypQQ4r+uyZkagKhRANCAASmUnEuTbs8DH0mHmAUqr9wvl71XrsY
        W/owZaiIeXw898m0imcDaAWjFC0BHj9C+eMngm/NzEiqS5bbd8dH2lBS
        -----END PRIVATE KEY-----
        """;

    public string PrivateKeyPemFile { get; set; } = "";

    public void HydrateFileBackedSecrets(string? contentRootPath = null)
    {
        PublicKeyPem = FileBackedSecretReader.ReadPemOrInline(
            PublicKeyPem,
            PublicKeyPemFile,
            $"{SectionName}:PublicKeyPemFile",
            contentRootPath);
        PrivateKeyPem = FileBackedSecretReader.ReadPemOrInline(
            PrivateKeyPem,
            PrivateKeyPemFile,
            $"{SectionName}:PrivateKeyPemFile",
            contentRootPath);
    }
}
