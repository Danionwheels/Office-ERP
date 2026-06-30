using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.SurveyValuation;

public sealed class VehicleDetails : ValueObject
{
    private VehicleDetails(
        string? make,
        string? registrationNumber,
        string? chassisNumber,
        string? model,
        string? engineNumber,
        SurveyReferenceCode? workshopCode)
    {
        Make = make;
        RegistrationNumber = registrationNumber;
        ChassisNumber = chassisNumber;
        Model = model;
        EngineNumber = engineNumber;
        WorkshopCode = workshopCode;
    }

    public string? Make { get; }

    public string? RegistrationNumber { get; }

    public string? ChassisNumber { get; }

    public string? Model { get; }

    public string? EngineNumber { get; }

    public SurveyReferenceCode? WorkshopCode { get; }

    public static VehicleDetails Create(
        string? make = null,
        string? registrationNumber = null,
        string? chassisNumber = null,
        string? model = null,
        string? engineNumber = null,
        SurveyReferenceCode? workshopCode = null)
    {
        return new VehicleDetails(
            Clean(make),
            Clean(registrationNumber),
            Clean(chassisNumber),
            Clean(model),
            Clean(engineNumber),
            workshopCode);
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Make;
        yield return RegistrationNumber;
        yield return ChassisNumber;
        yield return Model;
        yield return EngineNumber;
        yield return WorkshopCode;
    }
}
