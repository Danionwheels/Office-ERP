namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateProviderAccessOperator;

public sealed record CreateProviderAccessOperatorCommand(
    string Email,
    string FullName,
    string Password,
    string[] Scopes,
    string? CreatedBy);
