using SafarSuite.ControlDesk.Application.Modules.Diagnostics.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.Diagnostics.GetOfficeReadiness;

public sealed record GetOfficeReadinessResult(
    bool IsReady,
    string Status,
    string Code,
    OfficeDatabaseReadinessResult Database)
{
    public const string ReadyStatus = "Ready";

    public const string NotReadyStatus = "NotReady";
}
