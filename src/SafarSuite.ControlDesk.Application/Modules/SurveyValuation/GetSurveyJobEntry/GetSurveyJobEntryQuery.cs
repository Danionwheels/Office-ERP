namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.GetSurveyJobEntry;

public sealed record GetSurveyJobEntryQuery(Guid? SurveyJobId = null, string? SurveyJobNumber = null);
