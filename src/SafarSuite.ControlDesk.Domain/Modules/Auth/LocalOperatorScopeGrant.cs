namespace SafarSuite.ControlDesk.Domain.Modules.Auth;

public sealed class LocalOperatorScopeGrant
{
    private LocalOperatorScopeGrant()
    {
        Value = string.Empty;
    }

    private LocalOperatorScopeGrant(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }

    internal static LocalOperatorScopeGrant Create(string value) =>
        new(LocalOperatorScope.Normalize(value));
}
