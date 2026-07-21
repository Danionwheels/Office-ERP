namespace SafarSuite.ControlDesk.Infrastructure.System;

public sealed class ControlDeskClockOptions
{
    public const string SectionName = "ControlDesk:Clock";

    public string BusinessTimeZoneId { get; set; } = "Asia/Karachi";
}
