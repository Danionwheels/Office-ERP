using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SafarSuite.ControlDesk.Architecture.Tests;

internal sealed class BackendModuleDependencyScanner
{
    private static readonly Regex ModuleReferencePattern = new(
        @"(?:global::)?SafarSuite\.ControlDesk\.(?<layer>Domain|Application)\.Modules\.(?<module>[A-Za-z_][A-Za-z0-9_]*)(?<suffix>(?:\.[A-Za-z_][A-Za-z0-9_]*)*)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> ModuleRootAliasTargets = new(
        StringComparer.Ordinal)
    {
        "SafarSuite.ControlDesk",
        "SafarSuite.ControlDesk.Domain",
        "SafarSuite.ControlDesk.Application",
        "SafarSuite.ControlDesk.Domain.Modules",
        "SafarSuite.ControlDesk.Application.Modules"
    };

    public IReadOnlyList<BackendModuleDependency> ScanRepository(string repositoryRoot)
    {
        var roots = new[]
        {
            new ModuleRoot(
                "Domain",
                Path.Combine(repositoryRoot, "src", "SafarSuite.ControlDesk.Domain", "Modules")),
            new ModuleRoot(
                "Application",
                Path.Combine(repositoryRoot, "src", "SafarSuite.ControlDesk.Application", "Modules"))
        };

        var dependencies = new List<BackendModuleDependency>();

        foreach (var root in roots)
        {
            if (!Directory.Exists(root.Path))
            {
                throw new DirectoryNotFoundException($"Backend modules root does not exist: {root.Path}");
            }

            foreach (var file in Directory.EnumerateFiles(root.Path, "*.cs", SearchOption.AllDirectories)
                         .Where(path => !ContainsBuildDirectory(path)))
            {
                var relativeToModules = Path.GetRelativePath(root.Path, file);
                var sourceModule = relativeToModules.Split(
                    new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries)[0];
                var repositoryRelativeFile = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');

                dependencies.AddRange(ScanSource(
                    root.Area,
                    sourceModule,
                    repositoryRelativeFile,
                    File.ReadAllText(file)));
            }
        }

        return Normalize(dependencies);
    }

    public IReadOnlyList<BackendModuleDependency> ScanSource(
        string sourceArea,
        string sourceModule,
        string repositoryRelativeFile,
        string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: repositoryRelativeFile);
        var root = syntaxTree.GetRoot();
        var dependencies = new List<BackendModuleDependency>();

        foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            var referencedName = NormalizeGlobalAlias(usingDirective.Name?.ToString() ?? string.Empty);
            var lineNumber = GetLineNumber(syntaxTree, usingDirective.Span);
            var match = ModuleReferencePattern.Match(referencedName);

            if (match.Success)
            {
                AddIfPrivate(
                    dependencies,
                    sourceArea,
                    sourceModule,
                    repositoryRelativeFile,
                    match,
                    lineNumber,
                    usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword),
                    usingDirective.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)
                        ? $"global using {CanonicalReference(match)}"
                        : CanonicalReference(match));
                continue;
            }

            if (usingDirective.Alias is not null && ModuleRootAliasTargets.Contains(referencedName))
            {
                dependencies.Add(new BackendModuleDependency(
                    sourceArea,
                    sourceModule,
                    AliasTargetLayer(referencedName),
                    "UnresolvedAlias",
                    repositoryRelativeFile,
                    $"alias {usingDirective.Alias.Name.Identifier.ValueText} = {referencedName}",
                    lineNumber));
            }
        }

        var candidates = root.DescendantNodes()
            .Where(node => node is NameSyntax or MemberAccessExpressionSyntax)
            .Where(node => !node.AncestorsAndSelf().Any(
                ancestor => ancestor is UsingDirectiveSyntax or BaseNamespaceDeclarationSyntax))
            .Select(node => new SyntaxCandidate(node, ModuleReferencePattern.Match(node.ToString())))
            .Where(candidate => candidate.Match.Success)
            .OrderByDescending(candidate => candidate.Node.Span.Length)
            .ThenBy(candidate => candidate.Node.SpanStart)
            .ToArray();

        var acceptedSpans = new List<TextSpan>();
        foreach (var candidate in candidates)
        {
            if (acceptedSpans.Any(span => Contains(span, candidate.Node.Span)))
            {
                continue;
            }

            acceptedSpans.Add(candidate.Node.Span);
            AddIfPrivate(
                dependencies,
                sourceArea,
                sourceModule,
                repositoryRelativeFile,
                candidate.Match,
                GetLineNumber(syntaxTree, candidate.Node.Span),
                forceViolation: false,
                CanonicalReference(candidate.Match));
        }

        return Normalize(dependencies);
    }

    private static void AddIfPrivate(
        ICollection<BackendModuleDependency> dependencies,
        string sourceArea,
        string sourceModule,
        string repositoryRelativeFile,
        Match match,
        int lineNumber,
        bool forceViolation,
        string canonicalReference)
    {
        var targetLayer = match.Groups["layer"].Value;
        var targetModule = match.Groups["module"].Value;
        var suffix = match.Groups["suffix"].Value;
        var isSameModule = sourceModule.Equals(targetModule, StringComparison.Ordinal);
        var usesPublicGate = suffix.Equals(".Public", StringComparison.Ordinal) ||
                             suffix.StartsWith(".Public.", StringComparison.Ordinal);

        if (!forceViolation && (isSameModule || usesPublicGate))
        {
            return;
        }

        dependencies.Add(new BackendModuleDependency(
            sourceArea,
            sourceModule,
            targetLayer,
            targetModule,
            repositoryRelativeFile,
            canonicalReference,
            lineNumber));
    }

    private static IReadOnlyList<BackendModuleDependency> Normalize(
        IEnumerable<BackendModuleDependency> dependencies) =>
        dependencies
            .GroupBy(dependency => dependency.BaselineEntry, StringComparer.Ordinal)
            .Select(group => group.OrderBy(dependency => dependency.LineNumber).First())
            .OrderBy(dependency => dependency.BaselineEntry, StringComparer.Ordinal)
            .ToArray();

    private static string CanonicalReference(Match match) =>
        $"SafarSuite.ControlDesk.{match.Groups["layer"].Value}.Modules.{match.Groups["module"].Value}{match.Groups["suffix"].Value}";

    private static string NormalizeGlobalAlias(string value) =>
        value.StartsWith("global::", StringComparison.Ordinal) ? value[8..] : value;

    private static string AliasTargetLayer(string aliasTarget) =>
        aliasTarget.Contains(".Application", StringComparison.Ordinal)
            ? "Application"
            : aliasTarget.Contains(".Domain", StringComparison.Ordinal)
                ? "Domain"
                : "Unknown";

    private static int GetLineNumber(SyntaxTree syntaxTree, TextSpan span) =>
        syntaxTree.GetLineSpan(span).StartLinePosition.Line + 1;

    private static bool Contains(TextSpan outer, TextSpan inner) =>
        outer.Start <= inner.Start && outer.End >= inner.End;

    private static bool ContainsBuildDirectory(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "bin" or "obj");

    private sealed record ModuleRoot(string Area, string Path);

    private sealed record SyntaxCandidate(SyntaxNode Node, Match Match);
}
