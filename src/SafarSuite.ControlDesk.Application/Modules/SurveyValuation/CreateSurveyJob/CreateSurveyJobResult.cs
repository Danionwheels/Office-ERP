namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.CreateSurveyJob;

public sealed record CreateSurveyJobResult(
    Guid SurveyJobId,
    string SurveyJobNumber,
    string Status);
