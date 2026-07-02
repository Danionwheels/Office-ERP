namespace SafarSuite.ControlCloud.Application.Common;

public interface IControlCloudClock
{
    DateTimeOffset UtcNow { get; }
}
