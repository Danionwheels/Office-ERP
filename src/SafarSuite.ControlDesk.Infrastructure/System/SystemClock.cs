using SafarSuite.ControlDesk.Application.Common.Abstractions;

namespace SafarSuite.ControlDesk.Infrastructure.System;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}
