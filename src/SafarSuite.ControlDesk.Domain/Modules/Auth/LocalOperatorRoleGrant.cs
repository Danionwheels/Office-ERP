namespace SafarSuite.ControlDesk.Domain.Modules.Auth;

public sealed class LocalOperatorRoleGrant
{
    private LocalOperatorRoleGrant()
    {
        Value = string.Empty;
    }

    private LocalOperatorRoleGrant(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }

    internal static LocalOperatorRoleGrant Create(string value) =>
        new(LocalOperatorRole.Normalize(value));
}
