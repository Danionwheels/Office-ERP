namespace SafarSuite.ControlDesk.Architecture.Tests;

public sealed class BackendModuleBoundaryTests
{
    [Fact]
    public void Current_private_dependencies_match_the_checked_in_baseline()
    {
        var repositoryRoot = RepositoryRootLocator.Find();
        var baselinePath = Path.Combine(
            repositoryRoot,
            "tests",
            "SafarSuite.ControlDesk.Architecture.Tests",
            "Baselines",
            "BackendModulePrivateDependencies.txt");

        var expected = BackendModuleDependencyBaseline.Load(baselinePath);
        var actual = new BackendModuleDependencyScanner()
            .ScanRepository(repositoryRoot)
            .Select(dependency => dependency.BaselineEntry)
            .ToArray();
        var difference = BackendModuleDependencyBaseline.Compare(expected, actual);

        Assert.True(difference.IsMatch, difference.ToFailureMessage());
    }

    [Fact]
    public void Baseline_comparison_rejects_new_and_stale_entries()
    {
        var difference = BackendModuleDependencyBaseline.Compare(
            ["existing"],
            ["new"]);

        Assert.Equal(["new"], difference.Added);
        Assert.Equal(["existing"], difference.Removed);
        Assert.False(difference.IsMatch);
    }
}
