namespace SafarSuite.ConnectedAcceptance;

internal sealed class AcceptanceAssertions
{
    private readonly List<string> _passed = [];

    public IReadOnlyList<string> Passed => _passed;

    public void Equal<T>(T expected, T actual, string assertion)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new ConnectedAcceptanceFailureException(
                $"Assertion failed: {assertion}. Expected '{expected}', received '{actual}'.");
        }

        _passed.Add(assertion);
    }

    public void True(bool condition, string assertion)
    {
        if (!condition)
        {
            throw new ConnectedAcceptanceFailureException($"Assertion failed: {assertion}.");
        }

        _passed.Add(assertion);
    }

    public void NotEmpty(Guid value, string assertion)
    {
        True(value != Guid.Empty, assertion);
    }

    public void NotBlank(string? value, string assertion)
    {
        True(!string.IsNullOrWhiteSpace(value), assertion);
    }

    public void Balanced(decimal debit, decimal credit, string assertion)
    {
        True(debit > 0m && debit == credit, assertion);
    }
}
