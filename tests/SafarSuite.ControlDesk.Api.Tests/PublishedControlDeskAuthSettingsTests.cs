using System.Text.Json;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class PublishedControlDeskAuthSettingsTests
{
    [Fact]
    public void Base_settings_contain_no_operator_identity_or_session_signing_secret()
    {
        var repositoryRoot = FindRepositoryRoot();
        var settingsPath = Path.Combine(
            repositoryRoot,
            "src",
            "SafarSuite.ControlDesk.Api",
            "appsettings.json");
        using var settings = JsonDocument.Parse(File.ReadAllText(settingsPath));
        var operatorAccess = settings.RootElement
            .GetProperty("ControlDesk")
            .GetProperty("OperatorAccess");

        Assert.True(operatorAccess.TryGetProperty("SessionMinutes", out _));
        Assert.False(operatorAccess.TryGetProperty("SessionSigningSecret", out _));
        Assert.False(operatorAccess.TryGetProperty("Users", out _));
    }

    [Fact]
    public void Development_settings_explicitly_own_the_fixture_and_development_secret()
    {
        var repositoryRoot = FindRepositoryRoot();
        var settingsPath = Path.Combine(
            repositoryRoot,
            "src",
            "SafarSuite.ControlDesk.Api",
            "appsettings.Development.json");
        using var settings = JsonDocument.Parse(File.ReadAllText(settingsPath));
        var operatorAccess = settings.RootElement
            .GetProperty("ControlDesk")
            .GetProperty("OperatorAccess");

        Assert.True(operatorAccess.TryGetProperty("SessionSigningSecret", out _));
        Assert.True(operatorAccess.TryGetProperty("Users", out var users));
        Assert.Equal(JsonValueKind.Array, users.ValueKind);
        Assert.NotEqual(0, users.GetArrayLength());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SafarSuite.ControlDesk.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }
}
