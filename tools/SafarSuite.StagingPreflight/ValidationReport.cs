namespace SafarSuite.StagingPreflight;

public sealed record ValidationFailure(string Code, string Message);

public sealed class ValidationReport
{
    private readonly List<ValidationFailure> _failures = [];

    public IReadOnlyList<ValidationFailure> Failures => _failures;

    public bool IsValid => _failures.Count == 0;

    internal void Add(string code, string message) =>
        _failures.Add(new ValidationFailure(code, message));

    internal void AddRange(IEnumerable<ValidationFailure> failures) =>
        _failures.AddRange(failures);
}
