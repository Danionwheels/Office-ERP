namespace SafarSuite.ControlDesk.Architecture.Tests;

internal static class BackendModuleDependencyBaseline
{
    public static IReadOnlyList<string> Load(string path)
    {
        var entries = File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToArray();

        var sortedEntries = entries.Order(StringComparer.Ordinal).ToArray();
        if (!entries.SequenceEqual(sortedEntries, StringComparer.Ordinal))
        {
            throw new InvalidDataException($"Architecture baseline must be sorted: {path}");
        }

        if (entries.Distinct(StringComparer.Ordinal).Count() != entries.Length)
        {
            throw new InvalidDataException($"Architecture baseline contains duplicate entries: {path}");
        }

        return entries;
    }

    public static BackendModuleDependencyBaselineDifference Compare(
        IEnumerable<string> expected,
        IEnumerable<string> actual)
    {
        var expectedSet = expected.ToHashSet(StringComparer.Ordinal);
        var actualSet = actual.ToHashSet(StringComparer.Ordinal);

        return new BackendModuleDependencyBaselineDifference(
            actualSet.Except(expectedSet, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            expectedSet.Except(actualSet, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
    }
}

internal sealed record BackendModuleDependencyBaselineDifference(
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed)
{
    public bool IsMatch => Added.Count == 0 && Removed.Count == 0;

    public string ToFailureMessage()
    {
        var lines = new List<string>
        {
            "Backend private-module dependency baseline changed."
        };

        if (Added.Count > 0)
        {
            lines.Add("New private dependencies (route through Modules.<Target>.Public.*; do not baseline casually):");
            lines.AddRange(Added.Select(entry => $"  + {entry}"));
        }

        if (Removed.Count > 0)
        {
            lines.Add("Resolved dependencies (remove these stale entries from the checked-in baseline):");
            lines.AddRange(Removed.Select(entry => $"  - {entry}"));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
