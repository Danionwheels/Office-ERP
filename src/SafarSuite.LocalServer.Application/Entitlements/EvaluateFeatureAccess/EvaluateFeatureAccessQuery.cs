namespace SafarSuite.LocalServer.Application.Entitlements.EvaluateFeatureAccess;

public sealed record EvaluateFeatureAccessQuery(
    string ExpectedInstallationId,
    string ModuleCode,
    DateOnly? AsOfDate);
