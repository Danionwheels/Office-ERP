using SafarSuite.ControlDesk.Application.Common.Abstractions;

namespace SafarSuite.ControlDesk.Infrastructure.System;

public sealed class GuidIdGenerator : IIdGenerator
{
    public Guid NewGuid()
    {
        return Guid.NewGuid();
    }
}
