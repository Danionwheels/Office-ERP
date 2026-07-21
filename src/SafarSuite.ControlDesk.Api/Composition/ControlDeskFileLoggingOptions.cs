namespace SafarSuite.ControlDesk.Api.Composition;

public sealed class ControlDeskFileLoggingOptions
{
    public const string SectionName = "ControlDesk:Logging:File";

    public const int DefaultRetainedFileCountLimit = 50;

    public const int DefaultRetainedDays = 30;

    public bool Enabled { get; set; }

    public string? DirectoryPath { get; set; }

    public int RetainedFileCountLimit { get; set; } = DefaultRetainedFileCountLimit;

    public int RetainedDays { get; set; } = DefaultRetainedDays;
}
