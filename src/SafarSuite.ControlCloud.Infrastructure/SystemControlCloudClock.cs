using SafarSuite.ControlCloud.Application.Common;

namespace SafarSuite.ControlCloud.Infrastructure;

public sealed class SystemControlCloudClock : IControlCloudClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
