namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

public interface IControlCloudProviderAccessCredentialSource
{
    bool TryGetCredential(
        string configuredToken,
        string configuredSecret,
        out ControlCloudProviderAccessCredential credential);

    bool HasCredential(string configuredToken, string configuredSecret);
}

public sealed record ControlCloudProviderAccessCredential(
    string HeaderName,
    string HeaderValue);
