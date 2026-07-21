namespace SafarSuite.ControlDesk.Architecture.Tests;

public sealed class BackendModuleDependencyScannerTests
{
    private readonly BackendModuleDependencyScanner _scanner = new();

    [Theory]
    [InlineData("using SafarSuite.ControlDesk.Domain.Modules.Clients;")]
    [InlineData("using ClientPorts = SafarSuite.ControlDesk.Application.Modules.Clients.Ports;")]
    [InlineData("using static SafarSuite.ControlDesk.Domain.Modules.Clients.ClientId;")]
    [InlineData("internal sealed class Sample { private SafarSuite.ControlDesk.Domain.Modules.Clients.ClientId? _id; }")]
    public void Private_cross_module_reference_is_detected(string source)
    {
        var dependency = Assert.Single(Scan(source));

        Assert.Equal("Clients", dependency.TargetModule);
    }

    [Fact]
    public void Public_and_same_module_references_are_allowed_and_trivia_is_ignored()
    {
        const string source = """
            using SafarSuite.ControlDesk.Application.Modules.Clients.Public.Queries;
            using SafarSuite.ControlDesk.Domain.Modules.Billing;

            namespace SafarSuite.ControlDesk.Application.Modules.Billing.Sample;

            // SafarSuite.ControlDesk.Domain.Modules.Clients.ClientId is documentation only.
            internal sealed class Sample
            {
                private const string Text = "SafarSuite.ControlDesk.Domain.Modules.Clients.ClientId";
            }
            """;

        Assert.Empty(Scan(source));
    }

    [Fact]
    public void Publicity_namespace_is_not_treated_as_the_public_gate()
    {
        var dependency = Assert.Single(Scan(
            "using SafarSuite.ControlDesk.Domain.Modules.Clients.Publicity;"));

        Assert.Equal("Clients", dependency.TargetModule);
    }

    [Fact]
    public void Module_targeting_global_using_is_rejected_even_for_the_source_module()
    {
        var dependency = Assert.Single(Scan(
            "global using SafarSuite.ControlDesk.Domain.Modules.Billing.Public;"));

        Assert.StartsWith("global using ", dependency.CanonicalReference, StringComparison.Ordinal);
    }

    [Fact]
    public void Broad_module_root_alias_is_rejected_as_a_bypass()
    {
        var dependency = Assert.Single(Scan(
            "using ControlDeskDomain = SafarSuite.ControlDesk.Domain;"));

        Assert.Equal("UnresolvedAlias", dependency.TargetModule);
    }

    [Fact]
    public void Duplicate_reference_in_one_file_is_normalized()
    {
        const string source = """
            using SafarSuite.ControlDesk.Domain.Modules.Clients;
            using SafarSuite.ControlDesk.Domain.Modules.Clients;
            """;

        Assert.Single(Scan(source));
    }

    private IReadOnlyList<BackendModuleDependency> Scan(string source) =>
        _scanner.ScanSource("Application", "Billing", "synthetic/Sample.cs", source);
}
