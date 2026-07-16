using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class UserAllowance : ValueObject
{
    private UserAllowance()
    {
    }

    private UserAllowance(int? allowedNamedUsers, int? allowedConcurrentUsers)
    {
        AllowedNamedUsers = allowedNamedUsers;
        AllowedConcurrentUsers = allowedConcurrentUsers;
    }

    public int? AllowedNamedUsers { get; private set; }

    public int? AllowedConcurrentUsers { get; private set; }

    public static UserAllowance Unspecified { get; } = new(null, null);

    public static UserAllowance Create(int? allowedNamedUsers, int? allowedConcurrentUsers)
    {
        if (allowedNamedUsers < 0)
        {
            throw new ArgumentException("Allowed named-user count cannot be negative.", nameof(allowedNamedUsers));
        }

        if (allowedConcurrentUsers < 0)
        {
            throw new ArgumentException(
                "Allowed concurrent-user count cannot be negative.",
                nameof(allowedConcurrentUsers));
        }

        if (allowedNamedUsers.HasValue
            && allowedConcurrentUsers.HasValue
            && allowedConcurrentUsers.Value > allowedNamedUsers.Value)
        {
            throw new ArgumentException(
                "Allowed concurrent-user count cannot exceed the named-user count.",
                nameof(allowedConcurrentUsers));
        }

        return new UserAllowance(allowedNamedUsers, allowedConcurrentUsers);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AllowedNamedUsers;
        yield return AllowedConcurrentUsers;
    }
}
