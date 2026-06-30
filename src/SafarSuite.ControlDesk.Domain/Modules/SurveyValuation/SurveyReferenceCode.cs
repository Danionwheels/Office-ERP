using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

public sealed class SurveyReferenceCode : ValueObject
{
    private SurveyReferenceCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static SurveyReferenceCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Reference code is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 64)
        {
            throw new ArgumentException("Reference code cannot exceed 64 characters.", nameof(value));
        }

        return new SurveyReferenceCode(normalized);
    }

    public static SurveyReferenceCode? Optional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : Create(value);
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
