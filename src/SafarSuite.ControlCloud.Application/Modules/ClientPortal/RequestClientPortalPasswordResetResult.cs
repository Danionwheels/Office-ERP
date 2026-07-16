namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record RequestClientPortalPasswordResetResult(bool Accepted)
{
    public static RequestClientPortalPasswordResetResult GenericAccepted() => new(true);
}
