namespace SafarSuite.ControlDesk.Api.Modules.Auth;

public sealed class ControlDeskOperatorAccessOptions
{
    public const string SectionName = "ControlDesk:OperatorAccess";

    public int SessionMinutes { get; set; } = 480;

    public string SessionSigningKeyId { get; set; } = ConfiguredControlDeskSessionSigningKeyProvider.DefaultKeyId;

    public string SessionSigningSecret { get; set; } = string.Empty;

    public List<ControlDeskOperatorUserOptions> Users { get; set; } = [];
}

public sealed class ControlDeskOperatorUserOptions
{
    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";

    public List<string> Roles { get; set; } = [];

    public List<string> Scopes { get; set; } = [];
}
