using System.Text.Json;
using SafarSuite.ControlCloud.Api.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.AcknowledgeInstallationCommand;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetInstallationStatus;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetPendingInstallationCommands;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.QueueInstallationCommand;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ReportInstallationHeartbeat;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Api.Modules.LocalServer;

public static class LocalServerCommandEndpoints
{
    public static IEndpointRouteBuilder MapLocalServerCommandEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var controlCloudGroup = endpoints
            .MapGroup("/api/v1/control-cloud")
            .WithTags("Control Cloud Commands");

        controlCloudGroup.MapPost(
                "/clients/{clientId:guid}/installations/{installationId}/commands",
                QueueCommandAsync)
            .WithName("QueueInstallationCommand");

        controlCloudGroup.MapGet(
                "/clients/{clientId:guid}/installations/{installationId}/status",
                GetInstallationStatusAsync)
            .WithName("GetControlCloudInstallationStatus");

        var localServerGroup = endpoints
            .MapGroup("/api/v1/local-server")
            .WithTags("Local Server Commands");

        localServerGroup.MapGet(
                "/installations/{installationId}/commands/pending",
                GetPendingCommandsAsync)
            .WithName("GetPendingInstallationCommands");

        localServerGroup.MapPost(
                "/installations/{installationId}/commands/{commandId:guid}/acknowledgement",
                AcknowledgeCommandAsync)
            .WithName("AcknowledgeInstallationCommand");

        localServerGroup.MapPost(
                "/installations/{installationId}/heartbeat",
                ReportHeartbeatAsync)
            .WithName("ReportInstallationHeartbeat");

        localServerGroup.MapGet(
                "/installations/{installationId}/entitlement-bundle",
                GetEntitlementBundleAsync)
            .WithName("GetLocalServerEntitlementBundle");

        return endpoints;
    }

    private static async Task<IResult> GetInstallationStatusAsync(
        Guid clientId,
        string installationId,
        GetInstallationStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetInstallationStatusQuery(
                clientId,
                installationId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Status!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> QueueCommandAsync(
        Guid clientId,
        string installationId,
        QueueInstallationCommandRequest request,
        QueueInstallationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new QueueInstallationCommandCommand(
                clientId,
                installationId,
                request.CommandType,
                ToPayloadJson(request.Payload),
                request.NotBeforeUtc,
                request.ExpiresAtUtc,
                request.IdempotencyKey),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(ToResponse(result.Command!));
        }

        return ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> GetPendingCommandsAsync(
        string installationId,
        GetPendingInstallationCommandsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetPendingInstallationCommandsQuery(installationId),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(new PendingInstallationCommandsResponse(
                installationId.Trim(),
                result.Commands!
                    .OrderBy(command => command.CommandVersion)
                    .Select(ToResponse)
                    .ToArray()));
        }

        return ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> AcknowledgeCommandAsync(
        string installationId,
        Guid commandId,
        AcknowledgeInstallationCommandRequest request,
        AcknowledgeInstallationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new AcknowledgeInstallationCommandCommand(
                installationId,
                commandId,
                request.ResultStatus,
                request.Detail,
                ToPayloadJson(request.Payload)),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(ToResponse(result.Command!));
        }

        return ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> ReportHeartbeatAsync(
        string installationId,
        ReportLocalServerHeartbeatRequest request,
        ReportInstallationHeartbeatHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReportInstallationHeartbeatCommand(
                installationId,
                request.ClientId,
                request.LocalServerVersion,
                request.ReportedAtUtc,
                request.LicenseStatus,
                request.EntitlementVersion,
                request.PaidUntil,
                request.WarningStartsAt,
                request.GraceUntil,
                request.OfflineValidUntil,
                request.Detail),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(ToResponse(result.Heartbeat!));
        }

        return ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> GetEntitlementBundleAsync(
        string installationId,
        Guid clientId,
        GetClientPortalSignedEntitlementBundleHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetClientPortalSignedEntitlementBundleQuery(
                clientId,
                installationId),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(ClientPortalEndpoints.ToResponse(result.Bundle!));
        }

        return ToFailureResult(result.FailureCode, result.Detail);
    }

    private static IResult ToFailureResult(string? failureCode, string? detail)
    {
        var response = new { code = failureCode, detail };

        return failureCode switch
        {
            "EntitlementNotFound" => Results.NotFound(response),
            "InstallationNotFound" => Results.NotFound(response),
            "CommandNotFound" => Results.NotFound(response),
            "InstallationClientMismatch" => Results.Conflict(response),
            "EntitlementVersionRejected" => Results.Conflict(response),
            "InstallationCommandMismatch" => Results.Conflict(response),
            _ => Results.BadRequest(response)
        };
    }

    private static InstallationCommandResponse ToResponse(
        ControlCloudInstallationCommand command)
    {
        return new InstallationCommandResponse(
            command.CommandId,
            command.ClientId,
            command.InstallationId,
            command.CommandVersion,
            command.CommandType,
            command.Status,
            command.IdempotencyKey,
            ToPayload(command.PayloadJson),
            new InstallationCommandSignatureResponse(
                command.SignatureAlgorithm,
                command.SignatureKeyId,
                command.PayloadSha256,
                command.SignatureValue),
            command.QueuedAtUtc,
            command.NotBeforeUtc,
            command.ExpiresAtUtc,
            command.AcknowledgedAtUtc,
            command.AcknowledgementStatus,
            command.AcknowledgementDetail);
    }

    private static LocalServerHeartbeatResponse ToResponse(
        ControlCloudInstallationHeartbeat heartbeat)
    {
        return new LocalServerHeartbeatResponse(
            heartbeat.HeartbeatId,
            heartbeat.InstallationId,
            heartbeat.ClientId,
            heartbeat.HeartbeatStatus,
            heartbeat.ReceivedAtUtc,
            heartbeat.ReportedAtUtc,
            heartbeat.LicenseStatus,
            heartbeat.EntitlementVersion,
            heartbeat.PaidUntil,
            heartbeat.WarningStartsAt,
            heartbeat.GraceUntil,
            heartbeat.OfflineValidUntil,
            heartbeat.LocalServerVersion,
            heartbeat.Detail);
    }

    private static string ToPayloadJson(JsonElement payload)
    {
        return payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? "{}"
            : payload.GetRawText();
    }

    private static JsonElement ToPayload(string payloadJson)
    {
        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson);

        return document.RootElement.Clone();
    }
}
