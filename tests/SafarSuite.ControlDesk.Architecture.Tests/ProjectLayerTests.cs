using System.Xml.Linq;

namespace SafarSuite.ControlDesk.Architecture.Tests;

public sealed class ProjectLayerTests
{
    [Fact]
    public void Application_projects_do_not_reference_infrastructure()
    {
        var root = RepositoryRootLocator.Find();
        var violations = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path).Contains("Application", StringComparison.Ordinal))
            .SelectMany(path => XDocument.Load(path).Descendants("ProjectReference")
                .Select(reference => (Project: path, Target: (string?)reference.Attribute("Include") ?? string.Empty)))
            .Where(item => item.Target.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase))
            .Select(item => $"{Path.GetRelativePath(root, item.Project).Replace('\\', '/')} -> {item.Target}")
            .ToArray();

        Assert.True(violations.Length == 0, "Application layer references Infrastructure:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void Lower_layers_do_not_reference_upper_layers()
    {
        var root = RepositoryRootLocator.Find();
        var violations = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .SelectMany(path => XDocument.Load(path).Descendants("ProjectReference")
                .Select(reference => (Project: path, Target: (string?)reference.Attribute("Include") ?? string.Empty)))
            .Where(item => Path.GetFileName(item.Project).Contains("Domain", StringComparison.Ordinal)
                ? item.Target.Contains("Application", StringComparison.OrdinalIgnoreCase) || item.Target.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase) || item.Target.Contains("Api", StringComparison.OrdinalIgnoreCase)
                : Path.GetFileName(item.Project).Contains("Infrastructure", StringComparison.Ordinal) && item.Target.Contains("Api", StringComparison.OrdinalIgnoreCase))
            .Select(item => $"{Path.GetRelativePath(root, item.Project).Replace('\\', '/')} -> {item.Target}")
            .ToArray();

        Assert.True(violations.Length == 0, "Invalid project-layer references:\n" + string.Join("\n", violations));
    }
}
