namespace SafarSuite.ControlDesk.Architecture.Tests;

internal static class RepositoryRootLocator
{
    public static string Find()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SafarSuite.ControlDesk.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not locate SafarSuite.ControlDesk.sln above {AppContext.BaseDirectory}.");
    }
}
