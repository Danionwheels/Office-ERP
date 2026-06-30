namespace SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

public readonly record struct SurveyJobId(Guid Value)
{
    public static SurveyJobId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Survey job id cannot be empty.", nameof(value));
        }

        return new SurveyJobId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
