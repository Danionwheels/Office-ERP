using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class DeviceAllowance : ValueObject
{
    private DeviceAllowance(int allowedDevices)
    {
        AllowedDevices = allowedDevices;
    }

    public int AllowedDevices { get; }

    public static DeviceAllowance Create(int allowedDevices)
    {
        if (allowedDevices < 0)
        {
            throw new ArgumentException("Allowed device count cannot be negative.", nameof(allowedDevices));
        }

        return new DeviceAllowance(allowedDevices);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AllowedDevices;
    }
}
