using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

public sealed class SurveyAssignment : ValueObject
{
    private SurveyAssignment(
        SurveyReferenceCode? surveyorCode,
        SurveyReferenceCode? supervisorCode,
        SurveyReferenceCode? claimTypeCode,
        SurveyReferenceCode? requestSourceCode,
        SurveyReferenceCode? areaCode,
        SurveyReferenceCode? agencyCode)
    {
        SurveyorCode = surveyorCode;
        SupervisorCode = supervisorCode;
        ClaimTypeCode = claimTypeCode;
        RequestSourceCode = requestSourceCode;
        AreaCode = areaCode;
        AgencyCode = agencyCode;
    }

    public SurveyReferenceCode? SurveyorCode { get; }

    public SurveyReferenceCode? SupervisorCode { get; }

    public SurveyReferenceCode? ClaimTypeCode { get; }

    public SurveyReferenceCode? RequestSourceCode { get; }

    public SurveyReferenceCode? AreaCode { get; }

    public SurveyReferenceCode? AgencyCode { get; }

    public static SurveyAssignment Create(
        SurveyReferenceCode? surveyorCode = null,
        SurveyReferenceCode? supervisorCode = null,
        SurveyReferenceCode? claimTypeCode = null,
        SurveyReferenceCode? requestSourceCode = null,
        SurveyReferenceCode? areaCode = null,
        SurveyReferenceCode? agencyCode = null)
    {
        return new SurveyAssignment(
            surveyorCode,
            supervisorCode,
            claimTypeCode,
            requestSourceCode,
            areaCode,
            agencyCode);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SurveyorCode;
        yield return SupervisorCode;
        yield return ClaimTypeCode;
        yield return RequestSourceCode;
        yield return AreaCode;
        yield return AgencyCode;
    }
}
