namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed record BeginClientPortalMfaEnrollmentCommand(Guid UserId, string Password);
