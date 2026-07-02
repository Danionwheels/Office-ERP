using SafarSuite.LocalServer.Application.Common;

namespace SafarSuite.LocalServer.Infrastructure;

public sealed class SystemLocalServerClock : ILocalServerClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
