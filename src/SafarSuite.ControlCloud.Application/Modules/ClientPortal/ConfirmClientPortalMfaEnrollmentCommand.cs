namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record ConfirmClientPortalMfaEnrollmentCommand(Guid UserId, string Code);
