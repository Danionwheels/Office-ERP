namespace SafarSuite.ControlDesk.Api.Composition;

internal static class ControlDeskLogPathResolver
{
    public static string ResolveDirectory(string? configuredDirectoryPath)
    {
        var directoryPath = string.IsNullOrWhiteSpace(configuredDirectoryPath)
            ? ResolveDefaultDirectory()
            : Environment.ExpandEnvironmentVariables(configuredDirectoryPath.Trim());

        if (string.IsNullOrWhiteSpace(directoryPath) || !Path.IsPathRooted(directoryPath))
        {
            throw new InvalidOperationException(
                "Control Desk retained file logging requires an absolute directory path.");
        }

        try
        {
            return Path.GetFullPath(directoryPath);
        }
        catch (Exception exception) when (exception is ArgumentException
                                          or NotSupportedException
                                          or PathTooLongException)
        {
            throw new InvalidOperationException(
                "Control Desk retained file logging directory is invalid.");
        }
    }

    private static string ResolveDefaultDirectory()
    {
        var commonApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.CommonApplicationData);

        if (string.IsNullOrWhiteSpace(commonApplicationData))
        {
            throw new InvalidOperationException(
                "The operating system did not provide a shared application-data directory for retained logging.");
        }

        return Path.Combine(
            commonApplicationData,
            "SafarSuite",
            "ControlDesk",
            "Logs");
    }
}
