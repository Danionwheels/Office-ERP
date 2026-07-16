namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class ProductModuleCompatibility
{
    private ProductModuleCompatibility(
        string? minimumSafarSuiteVersion,
        string? minimumLocalServerVersion,
        IReadOnlyCollection<string> supportedDeploymentModes)
    {
        MinimumSafarSuiteVersion = minimumSafarSuiteVersion;
        MinimumLocalServerVersion = minimumLocalServerVersion;
        SupportedDeploymentModes = supportedDeploymentModes;
    }

    public string? MinimumSafarSuiteVersion { get; }

    public string? MinimumLocalServerVersion { get; }

    public IReadOnlyCollection<string> SupportedDeploymentModes { get; }

    public static ProductModuleCompatibility Any { get; } =
        new(null, null, Array.Empty<string>());

    public static ProductModuleCompatibility Create(
        string? minimumSafarSuiteVersion,
        string? minimumLocalServerVersion,
        IEnumerable<string>? supportedDeploymentModes = null)
    {
        var deploymentModes = NormalizeDeploymentModes(supportedDeploymentModes);

        return new ProductModuleCompatibility(
            NormalizeVersion(minimumSafarSuiteVersion, nameof(minimumSafarSuiteVersion)),
            NormalizeVersion(minimumLocalServerVersion, nameof(minimumLocalServerVersion)),
            deploymentModes);
    }

    private static string? NormalizeVersion(string? value, string parameterName)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length > 64)
        {
            throw new ArgumentException("Compatibility version cannot exceed 64 characters.", parameterName);
        }

        return normalized;
    }

    private static IReadOnlyCollection<string> NormalizeDeploymentModes(
        IEnumerable<string>? values)
    {
        var modes = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values ?? Array.Empty<string>())
        {
            var normalized = value.Trim();

            if (normalized.Length == 0)
            {
                continue;
            }

            if (normalized.Length > 64)
            {
                throw new ArgumentException(
                    "Supported deployment mode cannot exceed 64 characters.",
                    nameof(values));
            }

            if (seen.Add(normalized))
            {
                modes.Add(normalized);
            }
        }

        return modes.OrderBy(mode => mode, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
