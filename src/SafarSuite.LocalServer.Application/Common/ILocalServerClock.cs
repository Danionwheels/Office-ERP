namespace SafarSuite.LocalServer.Application.Common;

public interface ILocalServerClock
{
    DateTimeOffset UtcNow { get; }
}
