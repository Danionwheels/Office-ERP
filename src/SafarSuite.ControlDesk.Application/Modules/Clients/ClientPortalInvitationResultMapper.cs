using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.Clients;

internal static class ClientPortalInvitationResultMapper
{
    public static ClientPortalInvitationResult ToResult(ClientPortalInvitationResponse response)
    {
        return new ClientPortalInvitationResult(
            response.InvitationId,
            response.ClientId,
            response.Email,
            response.FullName,
            response.Role,
            response.Status,
            response.InvitedAtUtc,
            response.ExpiresAtUtc,
            response.InvitationToken,
            response.InvitationUrl);
    }

    public static ApplicationError ToApplicationError(ClientPortalInvitationClientResult result)
    {
        return ToApplicationError(result.FailureCode, result.Detail);
    }

    public static ApplicationError ToApplicationError(ClientPortalInvitationListClientResult result)
    {
        return ToApplicationError(result.FailureCode, result.Detail);
    }

    private static ApplicationError ToApplicationError(string? failureCode, string? detail)
    {
        return failureCode switch
        {
            "ClientNotFound" => ApplicationError.NotFound(
                "clientId",
                detail ?? "Client is not available in Control Cloud yet."),
            "InvitationNotFound" => ApplicationError.NotFound(
                "invitationId",
                detail ?? "Portal invitation was not found."),
            "PortalUserAlreadyExists" => ApplicationError.Conflict(
                "email",
                detail ?? "A portal user already exists for this client contact."),
            "InvitationClientMismatch" => ApplicationError.Conflict(
                "invitationId",
                detail ?? "Portal invitation belongs to another client."),
            "InvitationNotUsable" => ApplicationError.Conflict(
                "invitationId",
                detail ?? "Portal invitation cannot be changed."),
            "ControlCloudInvitationUnauthorized" => ApplicationError.ServiceUnavailable(
                detail ?? "Control Cloud rejected the provider invitation key."),
            "ControlCloudInvitationNotConfigured" => ApplicationError.ServiceUnavailable(
                detail ?? "Control Cloud invitation endpoint is not configured."),
            "ControlCloudInvitationUnavailable" => ApplicationError.ServiceUnavailable(
                detail ?? "Control Cloud invitation endpoint is unavailable."),
            _ => ApplicationError.Unexpected(
                detail ?? "Control Cloud could not process the portal invitation request.")
        };
    }
}
