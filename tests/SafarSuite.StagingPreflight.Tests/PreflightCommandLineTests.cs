using Xunit;

namespace SafarSuite.StagingPreflight.Tests;

public sealed class PreflightCommandLineTests
{
    [Fact]
    public void Parse_AcceptsCanonicalStagingDirectoryOption()
    {
        var result = PreflightCommandLine.Parse(
            ["--staging-directory", Path.Combine("custom", "staging")]);

        Assert.False(result.ShowHelp);
        Assert.Null(result.FailureMessage);
        Assert.Equal(Path.Combine("custom", "staging"), result.StagingDirectory);
    }
}
