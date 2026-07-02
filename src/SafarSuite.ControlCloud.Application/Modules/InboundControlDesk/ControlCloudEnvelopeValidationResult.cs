namespace SafarSuite.ControlCloud.Application.Modules.InboundControlDesk;

public sealed record ControlCloudEnvelopeValidationResult(
    bool IsValid,
    string? Code,
    string? Detail)
{
    public static ControlCloudEnvelopeValidationResult Valid()
    {
        return new ControlCloudEnvelopeValidationResult(true, null, null);
    }

    public static ControlCloudEnvelopeValidationResult Invalid(string code, string detail)
    {
        return new ControlCloudEnvelopeValidationResult(false, code, detail);
    }
}
