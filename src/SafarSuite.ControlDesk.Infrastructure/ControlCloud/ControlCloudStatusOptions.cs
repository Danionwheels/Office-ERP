namespace SafarSuite.ControlDesk.Infrastructure.ControlCloud;

public sealed class ControlCloudStatusOptions
{
    public const string SectionName = "ControlCloud:Status";

    public string BaseUrl { get; set; } = "http://localhost:5127";
}
