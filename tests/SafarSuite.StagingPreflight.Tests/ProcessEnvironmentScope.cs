namespace SafarSuite.StagingPreflight.Tests;

internal sealed class ProcessEnvironmentScope : IDisposable
{
    private readonly Dictionary<string, string?> _originalValues;

    public ProcessEnvironmentScope(IReadOnlyDictionary<string, string?> temporaryValues)
    {
        _originalValues = temporaryValues.Keys.ToDictionary(
            variable => variable,
            Environment.GetEnvironmentVariable,
            StringComparer.Ordinal);

        foreach (var (variable, value) in temporaryValues)
        {
            Environment.SetEnvironmentVariable(variable, value);
        }
    }

    public void Dispose()
    {
        foreach (var (variable, value) in _originalValues)
        {
            Environment.SetEnvironmentVariable(variable, value);
        }
    }
}
