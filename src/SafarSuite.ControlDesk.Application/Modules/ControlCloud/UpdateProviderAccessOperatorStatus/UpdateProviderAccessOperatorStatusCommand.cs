namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.UpdateProviderAccessOperatorStatus;

public sealed record UpdateProviderAccessOperatorStatusCommand(
    string UserId,
    string Status,
    string? UpdatedBy);
