namespace SafarSuite.ControlCloud.Infrastructure;

public sealed class ControlCloudReceiverOptions
{
    public const string SectionName = "ControlCloud:Receiver";

    public string ReceiptStorePath { get; set; } = "App_Data/control-cloud-receipts.jsonl";

    public string ProjectionStorePath { get; set; } = "App_Data/control-cloud-client-projections.json";

    public IReadOnlyCollection<ControlCloudReceiverSigningKeyOptions> SigningKeys { get; set; } =
        Array.Empty<ControlCloudReceiverSigningKeyOptions>();

    public void HydrateFileBackedSecrets(string? contentRootPath = null)
    {
        var keys = SigningKeys.ToArray();

        for (var index = 0; index < keys.Length; index++)
        {
            var key = keys[index];
            key.Secret = FileBackedSecretReader.ReadSecretOrInline(
                key.Secret,
                key.SecretFile,
                $"{SectionName}:SigningKeys:{index}:SecretFile",
                contentRootPath);
        }

        SigningKeys = keys;
    }
}

public sealed class ControlCloudReceiverSigningKeyOptions
{
    public string KeyId { get; set; } = "";

    public string Secret { get; set; } = "";

    public string SecretFile { get; set; } = "";
}
