using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;
using SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

namespace SafarSuite.ControlDesk.Application.Modules.SurveyValuation.UpdateSurveyJobDocuments;

public sealed class UpdateSurveyJobDocumentsValidator : IValidator<UpdateSurveyJobDocumentsCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(UpdateSurveyJobDocumentsCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.SurveyJobId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(nameof(value.SurveyJobId), "Survey job id is required."));
        }

        if (value.Documents is null)
        {
            errors.Add(ApplicationError.Validation(nameof(value.Documents), "Document checklist is required."));
            return errors;
        }

        var duplicateTypes = value.Documents
            .GroupBy(document => document.Type)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (var duplicateType in duplicateTypes)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.Documents),
                $"Document type {duplicateType} appears more than once."));
        }

        foreach (var document in value.Documents)
        {
            ValidateDocument(errors, document);
        }

        return errors;
    }

    private static void ValidateDocument(
        ICollection<ApplicationError> errors,
        SurveyJobDocumentChecklistItemCommand document)
    {
        if (!Enum.IsDefined(typeof(SurveyDocumentType), document.Type))
        {
            errors.Add(ApplicationError.Validation(nameof(document.Type), "Document type is not valid."));
        }

        if (!Enum.IsDefined(typeof(SurveyDocumentStatus), document.Status))
        {
            errors.Add(ApplicationError.Validation(nameof(document.Status), "Document status is not valid."));
        }

        if (document.Status != SurveyDocumentStatus.Received && document.ReceivedOn.HasValue)
        {
            errors.Add(ApplicationError.Validation(
                nameof(document.ReceivedOn),
                "Only received documents can have a received date."));
        }
    }
}
