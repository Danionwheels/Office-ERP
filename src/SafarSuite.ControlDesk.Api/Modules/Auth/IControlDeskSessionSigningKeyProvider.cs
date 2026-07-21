namespace SafarSuite.ControlDesk.Api.Modules.Auth;

public interface IControlDeskSessionSigningKeyProvider
{
    string SessionSigningKeyId { get; }

    byte[] CopySessionSigningKey();
}
