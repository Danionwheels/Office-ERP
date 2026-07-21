namespace SafarSuite.ControlDesk.Architecture.Tests;

internal sealed record BackendModuleDependency(
    string SourceArea,
    string SourceModule,
    string TargetLayer,
    string TargetModule,
    string RepositoryRelativeFile,
    string CanonicalReference,
    int LineNumber)
{
    public string BaselineEntry => string.Join(
        '|',
        SourceArea,
        SourceModule,
        TargetLayer,
        TargetModule,
        RepositoryRelativeFile,
        CanonicalReference);
}
