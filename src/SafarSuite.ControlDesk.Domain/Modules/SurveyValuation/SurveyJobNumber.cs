using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

public sealed class SurveyJobNumber : ValueObject
{
    private SurveyJobNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static SurveyJobNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Survey job number is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 32)
        {
            throw new ArgumentException("Survey job number cannot exceed 32 characters.", nameof(value));
        }

        return new SurveyJobNumber(normalized);
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
