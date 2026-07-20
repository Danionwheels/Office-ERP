using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Auth;

public sealed class LocalOperator : Entity<LocalOperatorId>
{
    private readonly List<string> _roles = [];
    private readonly List<string> _scopes = [];

    private LocalOperator()
    {
        Email = string.Empty;
        NormalizedEmail = string.Empty;
        FullName = string.Empty;
        PasswordHash = string.Empty;
    }

    private LocalOperator(
        LocalOperatorId id,
        LocalOperatorEmail email,
        string fullName,
        string passwordHash,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> scopes,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Email = email.Value;
        NormalizedEmail = email.NormalizedValue;
        FullName = fullName;
        PasswordHash = passwordHash;
        Status = LocalOperatorStatus.Active;
        SecurityVersion = 1;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        _roles.AddRange(roles);
        _scopes.AddRange(scopes);
    }

    public string Email { get; private set; }

    public string NormalizedEmail { get; private set; }

    public string FullName { get; private set; }

    public string PasswordHash { get; private set; }

    public LocalOperatorStatus Status { get; private set; }

    public long SecurityVersion { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();

    public IReadOnlyCollection<string> Scopes => _scopes.AsReadOnly();

    public static LocalOperator Create(
        LocalOperatorId id,
        LocalOperatorEmail email,
        string fullName,
        string passwordHash,
        IEnumerable<string> roles,
        IEnumerable<string> scopes,
        DateTimeOffset createdAtUtc)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Local operator id cannot be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(email);
        var cleanName = RequireText(fullName, nameof(fullName), 200);
        var cleanHash = RequireText(passwordHash, nameof(passwordHash), 512);
        var canonicalRoles = NormalizeValues(roles, LocalOperatorRole.Normalize, nameof(roles));
        var canonicalScopes = NormalizeValues(scopes, LocalOperatorScope.Normalize, nameof(scopes));
        ValidateAccessCombination(canonicalRoles, canonicalScopes);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        return new LocalOperator(
            id,
            email,
            cleanName,
            cleanHash,
            canonicalRoles,
            canonicalScopes,
            createdAtUtc);
    }

    public static LocalOperator CreateFirstAdministrator(
        LocalOperatorId id,
        LocalOperatorEmail email,
        string fullName,
        string passwordHash,
        DateTimeOffset createdAtUtc) =>
        Create(
            id,
            email,
            fullName,
            passwordHash,
            [LocalOperatorRole.Administrator],
            [LocalOperatorScope.Admin],
            createdAtUtc);

    public void Rename(string fullName, DateTimeOffset changedAtUtc)
    {
        var cleanName = RequireText(fullName, nameof(fullName), 200);
        EnsureChangeTime(changedAtUtc);

        if (string.Equals(FullName, cleanName, StringComparison.Ordinal))
        {
            return;
        }

        FullName = cleanName;
        RecordProtectedChange(changedAtUtc);
    }

    public void ChangePasswordHash(string passwordHash, DateTimeOffset changedAtUtc)
    {
        var cleanHash = RequireText(passwordHash, nameof(passwordHash), 512);
        EnsureChangeTime(changedAtUtc);

        if (string.Equals(PasswordHash, cleanHash, StringComparison.Ordinal))
        {
            return;
        }

        PasswordHash = cleanHash;
        RecordProtectedChange(changedAtUtc);
    }

    public void ChangeAccess(
        IEnumerable<string> roles,
        IEnumerable<string> scopes,
        DateTimeOffset changedAtUtc)
    {
        var canonicalRoles = NormalizeValues(roles, LocalOperatorRole.Normalize, nameof(roles));
        var canonicalScopes = NormalizeValues(scopes, LocalOperatorScope.Normalize, nameof(scopes));
        ValidateAccessCombination(canonicalRoles, canonicalScopes);
        EnsureChangeTime(changedAtUtc);

        if (_roles.SequenceEqual(canonicalRoles, StringComparer.Ordinal)
            && _scopes.SequenceEqual(canonicalScopes, StringComparer.Ordinal))
        {
            return;
        }

        _roles.Clear();
        _roles.AddRange(canonicalRoles);
        _scopes.Clear();
        _scopes.AddRange(canonicalScopes);
        RecordProtectedChange(changedAtUtc);
    }

    public void Disable(DateTimeOffset changedAtUtc)
    {
        EnsureChangeTime(changedAtUtc);

        if (Status == LocalOperatorStatus.Disabled)
        {
            return;
        }

        Status = LocalOperatorStatus.Disabled;
        RecordProtectedChange(changedAtUtc);
    }

    public void Enable(DateTimeOffset changedAtUtc)
    {
        EnsureChangeTime(changedAtUtc);

        if (Status == LocalOperatorStatus.Active)
        {
            return;
        }

        Status = LocalOperatorStatus.Active;
        RecordProtectedChange(changedAtUtc);
    }

    private static string[] NormalizeValues(
        IEnumerable<string> values,
        Func<string, string> normalize,
        string parameterName)
    {
        if (values is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var normalized = values
            .Select(normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one value is required.", parameterName);
        }

        return normalized;
    }

    private static void ValidateAccessCombination(
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> scopes)
    {
        var hasAdministratorRole = roles.Contains(LocalOperatorRole.Administrator, StringComparer.Ordinal);
        var hasAdminScope = scopes.Contains(LocalOperatorScope.Admin, StringComparer.Ordinal);

        if (hasAdministratorRole != hasAdminScope)
        {
            throw new ArgumentException(
                "The Administrator role and control-desk:admin scope must be granted or removed together.");
        }
    }

    private static string RequireText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        var cleaned = value.Trim();

        if (cleaned.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return cleaned;
    }

    private void EnsureChangeTime(DateTimeOffset changedAtUtc)
    {
        EnsureUtc(changedAtUtc, nameof(changedAtUtc));

        if (changedAtUtc < UpdatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(changedAtUtc),
                "Local operator changes cannot precede the last update time.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Local operator timestamps must be UTC.", parameterName);
        }
    }

    private void RecordProtectedChange(DateTimeOffset changedAtUtc)
    {
        SecurityVersion = checked(SecurityVersion + 1);
        UpdatedAtUtc = changedAtUtc;
    }
}
