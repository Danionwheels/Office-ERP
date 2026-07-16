using Microsoft.Extensions.Options;
using SafarSuite.ControlDesk.Application.Common.Abstractions;

namespace SafarSuite.ControlDesk.Infrastructure.System;

public sealed class SystemClock : IClock
{
    private readonly TimeZoneInfo _businessTimeZone;

    public SystemClock()
        : this(TimeZoneInfo.Local)
    {
    }

    public SystemClock(IOptions<ControlDeskClockOptions> options)
        : this(ResolveTimeZone(options.Value.BusinessTimeZoneId))
    {
    }

    private SystemClock(TimeZoneInfo businessTimeZone)
    {
        _businessTimeZone = businessTimeZone;
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(UtcNow, _businessTimeZone).DateTime);

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new InvalidOperationException("Control Desk business time zone id is required.");
        }

        var normalizedTimeZoneId = timeZoneId.Trim();
        var candidateIds = new List<string> { normalizedTimeZoneId };

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(normalizedTimeZoneId, out var windowsTimeZoneId))
        {
            candidateIds.Add(windowsTimeZoneId);
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(normalizedTimeZoneId, out var ianaTimeZoneId))
        {
            candidateIds.Add(ianaTimeZoneId);
        }

        foreach (var candidateId in candidateIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidateId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException exception)
            {
                throw new InvalidOperationException(
                    $"Control Desk business time zone '{timeZoneId}' is invalid on this host.",
                    exception);
            }
        }

        throw new InvalidOperationException(
            $"Control Desk business time zone '{timeZoneId}' was not found on this host.");
    }
}
