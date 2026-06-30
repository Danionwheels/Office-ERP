using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

public sealed class SurveyDocumentChecklistItem : ValueObject
{
    private SurveyDocumentChecklistItem(SurveyDocumentType type, SurveyDocumentStatus status, DateOnly? receivedOn)
    {
        Type = type;
        Status = status;
        ReceivedOn = receivedOn;
    }

    public SurveyDocumentType Type { get; }

    public SurveyDocumentStatus Status { get; }

    public DateOnly? ReceivedOn { get; }

    public static SurveyDocumentChecklistItem Create(
        SurveyDocumentType type,
        SurveyDocumentStatus status,
        DateOnly? receivedOn = null)
    {
        if (status != SurveyDocumentStatus.Received && receivedOn.HasValue)
        {
            throw new ArgumentException("Only received documents can have a received date.", nameof(receivedOn));
        }

        return new SurveyDocumentChecklistItem(type, status, receivedOn);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Status;
        yield return ReceivedOn;
    }
}
