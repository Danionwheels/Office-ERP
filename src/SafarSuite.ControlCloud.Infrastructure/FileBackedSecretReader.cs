namespace SafarSuite.ControlCloud.Infrastructure;

internal static class FileBackedSecretReader
{
    public static string ReadSecretOrInline(
        string inlineSecret,
        string secretFile,
        string optionName,
        string? contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(secretFile))
        {
            return inlineSecret;
        }

        var secretPath = ResolveSecretPath(secretFile, contentRootPath);

        if (!File.Exists(secretPath))
        {
            throw new InvalidOperationException(
                $"{optionName} points to a secret file that does not exist: {secretPath}");
        }

        var secret = File.ReadAllText(secretPath).Trim();

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                $"{optionName} points to an empty secret file: {secretPath}");
        }

        return secret;
    }

    public static string ReadPemOrInline(
        string inlinePem,
        string pemFile,
        string optionName,
        string? contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(pemFile))
        {
            return inlinePem;
        }

        var pemPath = ResolveSecretPath(pemFile, contentRootPath);

        if (!File.Exists(pemPath))
        {
            throw new InvalidOperationException(
                $"{optionName} points to a PEM file that does not exist: {pemPath}");
        }

        var pem = File.ReadAllText(pemPath).Trim();

        if (string.IsNullOrWhiteSpace(pem))
        {
            throw new InvalidOperationException(
                $"{optionName} points to an empty PEM file: {pemPath}");
        }

        return pem;
    }

    private static string ResolveSecretPath(string secretFile, string? contentRootPath)
    {
        var trimmed = secretFile.Trim();

        return Path.IsPathRooted(trimmed)
            ? Path.GetFullPath(trimmed)
            : Path.GetFullPath(Path.Combine(
                string.IsNullOrWhiteSpace(contentRootPath)
                    ? Directory.GetCurrentDirectory()
                    : contentRootPath,
                trimmed));
    }
}
