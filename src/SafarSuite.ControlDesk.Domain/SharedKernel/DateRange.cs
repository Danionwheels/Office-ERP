namespace SafarSuite.ControlDesk.Domain.SharedKernel;

public sealed class DateRange : ValueObject
{
    private DateRange()
    {
    }

    private DateRange(DateOnly startsOn, DateOnly endsOn)
    {
        StartsOn = startsOn;
        EndsOn = endsOn;
    }

    public DateOnly StartsOn { get; private set; }

    public DateOnly EndsOn { get; private set; }

    public static DateRange Create(DateOnly startsOn, DateOnly endsOn)
    {
        if (endsOn < startsOn)
        {
            throw new ArgumentException("End date cannot be before start date.", nameof(endsOn));
        }

        return new DateRange(startsOn, endsOn);
    }

    public bool Contains(DateOnly date)
    {
        return date >= StartsOn && date <= EndsOn;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartsOn;
        yield return EndsOn;
    }
}
