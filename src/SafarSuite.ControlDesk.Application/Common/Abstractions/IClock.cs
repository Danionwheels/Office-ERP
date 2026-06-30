namespace SafarSuite.ControlDesk.Application.Common.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateOnly Today { get; }
}
