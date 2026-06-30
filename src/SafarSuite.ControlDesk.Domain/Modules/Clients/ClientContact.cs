using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Clients;

public sealed class ClientContact : Entity<ClientContactId>
{
    private ClientContact(
        ClientContactId id,
        ClientContactRole role,
        string fullName,
        string? jobTitle,
        string? email,
        string? phone,
        bool isPrimary,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Role = role;
        FullName = fullName;
        JobTitle = jobTitle;
        Email = email;
        Phone = phone;
        IsPrimary = isPrimary;
        CreatedAtUtc = createdAtUtc;
    }

    public ClientContactRole Role { get; }

    public string FullName { get; }

    public string? JobTitle { get; }

    public string? Email { get; }

    public string? Phone { get; }

    public bool IsPrimary { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static ClientContact Create(
        ClientContactId id,
        ClientContactRole role,
        string fullName,
        string? jobTitle,
        string? email,
        string? phone,
        bool isPrimary,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Client contact full name is required.", nameof(fullName));
        }

        var cleanEmail = CleanOptional(email);
        var cleanPhone = CleanOptional(phone);

        if (cleanEmail is null && cleanPhone is null)
        {
            throw new ArgumentException("Client contact requires an email or phone.", nameof(email));
        }

        if (cleanEmail is not null && !cleanEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Client contact email is invalid.", nameof(email));
        }

        return new ClientContact(
            id,
            role,
            fullName.Trim(),
            CleanOptional(jobTitle),
            cleanEmail,
            cleanPhone,
            isPrimary,
            createdAtUtc);
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
