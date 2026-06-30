using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

public sealed class InsuredParty : ValueObject
{
    private InsuredParty(
        string name,
        string? phone,
        string? email,
        string? address,
        string? cnic,
        string? contactPerson,
        SurveyReferenceCode? contactDesignationCode,
        string? referenceNumber,
        string? ccNumber)
    {
        Name = name;
        Phone = phone;
        Email = email;
        Address = address;
        Cnic = cnic;
        ContactPerson = contactPerson;
        ContactDesignationCode = contactDesignationCode;
        ReferenceNumber = referenceNumber;
        CcNumber = ccNumber;
    }

    public string Name { get; }

    public string? Phone { get; }

    public string? Email { get; }

    public string? Address { get; }

    public string? Cnic { get; }

    public string? ContactPerson { get; }

    public SurveyReferenceCode? ContactDesignationCode { get; }

    public string? ReferenceNumber { get; }

    public string? CcNumber { get; }

    public static InsuredParty Create(
        string name,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? cnic = null,
        string? contactPerson = null,
        SurveyReferenceCode? contactDesignationCode = null,
        string? referenceNumber = null,
        string? ccNumber = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Insured name is required.", nameof(name));
        }

        return new InsuredParty(
            name.Trim(),
            Clean(phone),
            Clean(email),
            Clean(address),
            Clean(cnic),
            Clean(contactPerson),
            contactDesignationCode,
            Clean(referenceNumber),
            Clean(ccNumber));
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Phone;
        yield return Email;
        yield return Address;
        yield return Cnic;
        yield return ContactPerson;
        yield return ContactDesignationCode;
        yield return ReferenceNumber;
        yield return CcNumber;
    }
}
