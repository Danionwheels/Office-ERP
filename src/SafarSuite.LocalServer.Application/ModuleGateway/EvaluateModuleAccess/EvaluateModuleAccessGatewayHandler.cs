using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Entitlements.EvaluateFeatureAccess;

namespace SafarSuite.LocalServer.Application.ModuleGateway.EvaluateModuleAccess;

public sealed class EvaluateModuleAccessGatewayHandler
{
    private readonly EvaluateFeatureAccessHandler _featureAccessHandler;
    private readonly ILocalServerClock _clock;

    public EvaluateModuleAccessGatewayHandler(
        EvaluateFeatureAccessHandler featureAccessHandler,
        ILocalServerClock clock)
    {
        _featureAccessHandler = featureAccessHandler;
        _clock = clock;
    }

    public async Task<EvaluateModuleAccessGatewayResult> HandleAsync(
        EvaluateModuleAccessGatewayCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeRequiredText(command.InstallationId, 160);
        var moduleCode = NormalizeRequiredText(command.ModuleCode, 80);

        if (installationId is null)
        {
            return EvaluateModuleAccessGatewayResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before evaluating module access.");
        }

        if (moduleCode is null)
        {
            return EvaluateModuleAccessGatewayResult.Failure(
                "ModuleCodeRequired",
                "Module code is required before evaluating module access.");
        }

        try
        {
            var decision = await _featureAccessHandler.HandleAsync(
                new EvaluateFeatureAccessQuery(
                    installationId,
                    moduleCode,
                    command.AsOfDate),
                cancellationToken);

            return EvaluateModuleAccessGatewayResult.Success(
                new LocalServerModuleAccessResponse(
                    LocalServerModuleGatewayFormat.Version,
                    installationId,
                    decision.ModuleCode,
                    decision.IsAllowed,
                    decision.AccessState,
                    decision.Reason,
                    decision.EntitlementVersion,
                    decision.PaidUntil,
                    decision.WarningStartsAt,
                    decision.GraceUntil,
                    decision.OfflineValidUntil,
                    _clock.UtcNow));
        }
        catch (ArgumentException exception)
        {
            return EvaluateModuleAccessGatewayResult.Failure(
                "ModuleAccessEvaluationInvalid",
                exception.Message);
        }
    }

    private static string? NormalizeRequiredText(string? value, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
