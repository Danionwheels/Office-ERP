using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Commands.ProcessInstallationCommandsFromBootstrapConfiguration;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Application.Entitlements.PullEntitlementFromBootstrapConfiguration;
using SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatFromBootstrapConfiguration;
using SafarSuite.LocalServer.Application.ModuleGateway.EvaluateModuleAccess;
using SafarSuite.LocalServer.Application.Registration.Ports;
using SafarSuite.LocalServer.Application.Registration.RegisterInstallationFromBootstrapBundle;
using SafarSuite.LocalServer.Domain.Entitlements;
using SafarSuite.LocalServer.Domain.Registration;

namespace SafarSuite.LocalServer.Api.Modules.LocalServer;

public static class LocalServerRuntimeEndpoints
{
    public static IEndpointRouteBuilder MapLocalServerRuntimeEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/local-server")
            .WithTags("Local Server Runtime");

        group.MapGet("/bootstrap", GetBootstrapStatusAsync)
            .WithName("GetLocalServerBootstrapStatus");

        group.MapPost("/bootstrap-package/import", ImportBootstrapPackageAsync)
            .WithName("ImportLocalServerBootstrapPackage");

        group.MapPost("/entitlement/pull", PullEntitlementAsync)
            .WithName("PullLocalServerEntitlement");

        group.MapPost("/heartbeat", ReportHeartbeatAsync)
            .WithName("ReportLocalServerHeartbeatFromBootstrap");

        group.MapPost("/commands/process", ProcessCommandsAsync)
            .WithName("ProcessLocalServerCommands");

        group.MapPost("/modules/access", EvaluateModuleAccessAsync)
            .WithName("EvaluateLocalServerModuleAccess");

        group.MapGet("/modules/{moduleCode}/access", EvaluateModuleAccessFromBootstrapAsync)
            .WithName("EvaluateLocalServerModuleAccessFromBootstrap");

        return endpoints;
    }

    private static async Task<IResult> GetBootstrapStatusAsync(
        ILocalServerBootstrapConfigurationStore configurationStore,
        ILocalServerEntitlementCache entitlementCache,
        ILocalServerEntitlementTrustStateStore trustStateStore,
        CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetCurrentAsync(cancellationToken);
        var entitlement = await entitlementCache.GetCurrentAsync(cancellationToken);
        LocalServerEntitlementTrustState? trustState = null;

        if (configuration is not null)
        {
            trustState = await trustStateStore.GetAsync(
                configuration.InstallationId,
                cancellationToken);
        }

        return Results.Ok(ToStatusResponse(configuration, entitlement, trustState));
    }

    private static async Task<IResult> ImportBootstrapPackageAsync(
        LocalServerSignedBootstrapBundleResponse request,
        string? expectedInstallationId,
        RegisterInstallationFromBootstrapBundleHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RegisterInstallationFromBootstrapBundleCommand(
                request,
                expectedInstallationId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new LocalServerBootstrapImportResponse(
                result.BootstrapConfiguration!.ClientId,
                result.BootstrapConfiguration.InstallationId,
                result.BootstrapConfiguration.RegistrationStatus,
                result.BootstrapConfiguration.DeploymentProfile,
                result.Registration!.InstallationStatus,
                result.Registration.RegisteredAtUtc,
                result.BootstrapConfiguration.SignatureKeyId,
                result.BootstrapConfiguration.PayloadSha256))
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> PullEntitlementAsync(
        PullEntitlementFromBootstrapConfigurationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new PullEntitlementFromBootstrapConfigurationCommand(),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new
            {
                result.PulledAtUtc,
                result.Entitlement!.ClientId,
                result.Entitlement.InstallationId,
                result.Entitlement.EntitlementVersion,
                result.Entitlement.PaidUntil,
                result.Entitlement.OfflineValidUntil
            })
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> ReportHeartbeatAsync(
        ReportHeartbeatFromBootstrapConfigurationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReportHeartbeatFromBootstrapConfigurationCommand(
                Detail: "Manual local-server heartbeat."),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new
            {
                result.Heartbeat!.ClientId,
                result.Heartbeat.InstallationId,
                result.Heartbeat.HeartbeatStatus,
                result.Heartbeat.LicenseStatus,
                result.Heartbeat.EntitlementVersion,
                result.Heartbeat.ReceivedAtUtc,
                result.EntitlementState
            })
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> ProcessCommandsAsync(
        ProcessInstallationCommandsFromBootstrapConfigurationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ProcessInstallationCommandsFromBootstrapConfigurationCommand(),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new
            {
                result.PendingCommandCount,
                result.AppliedCount,
                result.FailedCount,
                result.RejectedCount,
                result.Commands
            })
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> EvaluateModuleAccessAsync(
        LocalServerModuleAccessRequest request,
        EvaluateModuleAccessGatewayHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new EvaluateModuleAccessGatewayCommand(
                request.InstallationId,
                request.ModuleCode,
                request.AsOfDate,
                request.RequestedBy),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Access!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> EvaluateModuleAccessFromBootstrapAsync(
        string moduleCode,
        string? asOfDate,
        string? requestedBy,
        ILocalServerBootstrapConfigurationStore configurationStore,
        EvaluateModuleAccessGatewayHandler handler,
        CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetCurrentAsync(cancellationToken);

        if (configuration is null)
        {
            return ToFailureResult(
                "BootstrapConfigurationMissing",
                "A verified bootstrap configuration is required before evaluating module access.");
        }

        if (!TryParseDateOnly(asOfDate, out var parsedDate, out var failure))
        {
            return ToFailureResult(
                "AsOfDateInvalid",
                failure ?? "asOfDate must use yyyy-MM-dd format.");
        }

        var result = await handler.HandleAsync(
            new EvaluateModuleAccessGatewayCommand(
                configuration.InstallationId,
                moduleCode,
                parsedDate,
                requestedBy),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Access!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static LocalServerRuntimeStatusResponse ToStatusResponse(
        LocalServerBootstrapConfiguration? configuration,
        LocalServerCachedEntitlement? entitlement,
        LocalServerEntitlementTrustState? trustState)
    {
        return new LocalServerRuntimeStatusResponse(
            HasBootstrapConfiguration: configuration is not null,
            ClientId: configuration?.ClientId,
            InstallationId: configuration?.InstallationId,
            RegistrationStatus: configuration?.RegistrationStatus,
            DeploymentProfile: configuration?.DeploymentProfile,
            CloudBaseUrl: configuration?.CloudBaseUrl,
            LocalServerVersion: configuration?.LocalServerVersion,
            SetupTokenExpiresAtUtc: configuration?.SetupTokenExpiresAtUtc,
            LastRegistrationAttemptUtc: configuration?.LastRegistrationAttemptUtc,
            LastRegistrationSucceededAtUtc: configuration?.LastRegistrationSucceededAtUtc,
            LastRegistrationFailureCode: configuration?.LastRegistrationFailureCode,
            HasCachedEntitlement: entitlement is not null,
            EntitlementVersion: entitlement?.EntitlementVersion,
            PaidUntil: entitlement?.PaidUntil,
            OfflineValidUntil: entitlement?.OfflineValidUntil,
            LastSuccessfulCloudTimeUtc: trustState?.LastSuccessfulCloudTimeUtc,
            LastLocalCheckAtUtc: trustState?.LastLocalCheckAtUtc,
            ClockMovedBackwards: trustState?.ClockMovedBackwards ?? false);
    }

    private static IResult ToFailureResult(
        string? failureCode,
        string? detail)
    {
        var code = string.IsNullOrWhiteSpace(failureCode)
            ? "LocalServerRequestFailed"
            : failureCode;
        var response = new LocalServerFailureResponse(
            code,
            string.IsNullOrWhiteSpace(detail)
                ? "Local server request failed."
                : detail);

        return code switch
        {
            "BootstrapConfigurationMissing" => Results.Conflict(response),
            "ControlCloudUnavailable" => Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable),
            "ControlCloudTimeout" => Results.Json(response, statusCode: StatusCodes.Status504GatewayTimeout),
            "ControlCloudRegistrationFailed" => Results.Json(response, statusCode: StatusCodes.Status502BadGateway),
            "ControlCloudPullFailed" => Results.Json(response, statusCode: StatusCodes.Status502BadGateway),
            "ControlCloudHeartbeatFailed" => Results.Json(response, statusCode: StatusCodes.Status502BadGateway),
            "ControlCloudCommandPullFailed" => Results.Json(response, statusCode: StatusCodes.Status502BadGateway),
            "ControlCloudCommandAcknowledgementFailed" => Results.Json(response, statusCode: StatusCodes.Status502BadGateway),
            "ControlCloudCommandResponseInvalid" => Results.Json(response, statusCode: StatusCodes.Status502BadGateway),
            "SetupTokenNotFound" => Results.Json(response, statusCode: StatusCodes.Status401Unauthorized),
            "CommandNotFound" => Results.NotFound(response),
            "SignatureInvalid" => Results.BadRequest(response),
            "SigningKeyUnknown" => Results.BadRequest(response),
            "PayloadHashMismatch" => Results.BadRequest(response),
            "InstallationMismatch" => Results.Conflict(response),
            "CommandClientMismatch" => Results.Conflict(response),
            "CommandInstallationMismatch" => Results.Conflict(response),
            "CommandStatusInvalid" => Results.Conflict(response),
            _ when code.EndsWith("Required", StringComparison.Ordinal) => Results.BadRequest(response),
            _ when code.EndsWith("Invalid", StringComparison.Ordinal) => Results.BadRequest(response),
            _ when code.EndsWith("Unsupported", StringComparison.Ordinal) => Results.BadRequest(response),
            _ => Results.BadRequest(response)
        };
    }

    private static bool TryParseDateOnly(
        string? value,
        out DateOnly? date,
        out string? failure)
    {
        date = null;
        failure = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (DateOnly.TryParse(value.Trim(), out var parsedDate))
        {
            date = parsedDate;
            return true;
        }

        failure = "asOfDate must use yyyy-MM-dd format.";
        return false;
    }
}

public sealed record LocalServerRuntimeStatusResponse(
    bool HasBootstrapConfiguration,
    Guid? ClientId,
    string? InstallationId,
    string? RegistrationStatus,
    LocalServerBootstrapDeploymentProfile? DeploymentProfile,
    string? CloudBaseUrl,
    string? LocalServerVersion,
    DateTimeOffset? SetupTokenExpiresAtUtc,
    DateTimeOffset? LastRegistrationAttemptUtc,
    DateTimeOffset? LastRegistrationSucceededAtUtc,
    string? LastRegistrationFailureCode,
    bool HasCachedEntitlement,
    long? EntitlementVersion,
    DateOnly? PaidUntil,
    DateOnly? OfflineValidUntil,
    DateTimeOffset? LastSuccessfulCloudTimeUtc,
    DateTimeOffset? LastLocalCheckAtUtc,
    bool ClockMovedBackwards);

public sealed record LocalServerBootstrapImportResponse(
    Guid ClientId,
    string InstallationId,
    string BootstrapRegistrationStatus,
    LocalServerBootstrapDeploymentProfile DeploymentProfile,
    string CloudRegistrationStatus,
    DateTimeOffset RegisteredAtUtc,
    string SignatureKeyId,
    string PayloadSha256);

public sealed record LocalServerFailureResponse(
    string FailureCode,
    string Detail);
