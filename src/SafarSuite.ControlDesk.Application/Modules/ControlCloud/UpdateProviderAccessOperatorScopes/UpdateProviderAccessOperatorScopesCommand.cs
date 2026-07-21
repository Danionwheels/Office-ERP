namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.UpdateProviderAccessOperatorScopes;

public sealed record UpdateProviderAccessOperatorScopesCommand(
    string UserId,
    string[] Scopes,
    string? UpdatedBy);
