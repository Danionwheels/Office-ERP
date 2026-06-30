using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJobDocuments;

public sealed record UpdateSurveyJobDocumentsCommand(
    Guid SurveyJobId,
    IReadOnlyCollection<SurveyJobDocumentChecklistItemCommand>? Documents);

public sealed record SurveyJobDocumentChecklistItemCommand(
    SurveyDocumentType Type,
    SurveyDocumentStatus Status,
    DateOnly? ReceivedOn);
