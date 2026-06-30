using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Clients;

public sealed class SupportNote : ValueObject
{
    private SupportNote(string text, string createdBy, DateTimeOffset createdAtUtc)
    {
        Text = text;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
    }

    public string Text { get; }

    public string CreatedBy { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static SupportNote Create(string text, string createdBy, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Support note text is required.", nameof(text));
        }

        if (string.IsNullOrWhiteSpace(createdBy))
        {
            throw new ArgumentException("Support note author is required.", nameof(createdBy));
        }

        return new SupportNote(text.Trim(), createdBy.Trim(), createdAtUtc);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Text;
        yield return CreatedBy;
        yield return CreatedAtUtc;
    }
}
