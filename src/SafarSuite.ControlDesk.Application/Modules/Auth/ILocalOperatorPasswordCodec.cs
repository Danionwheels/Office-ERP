namespace SafarSuite.ControlDesk.Application.Modules.Auth;

public interface ILocalOperatorPasswordCodec
{
    string Hash(string password);

    bool Verify(string password, string? passwordHash);
}
