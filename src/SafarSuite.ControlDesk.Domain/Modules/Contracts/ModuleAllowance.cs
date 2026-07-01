using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class ModuleAllowance : ValueObject
{
    private ModuleAllowance()
    {
        ModuleCode = null!;
    }

    private ModuleAllowance(ModuleCode moduleCode, bool isEnabled)
    {
        ModuleCode = moduleCode;
        IsEnabled = isEnabled;
    }

    public ModuleCode ModuleCode { get; private set; }

    public bool IsEnabled { get; private set; }

    public static ModuleAllowance Enabled(ModuleCode moduleCode)
    {
        return new ModuleAllowance(moduleCode, isEnabled: true);
    }

    public static ModuleAllowance Disabled(ModuleCode moduleCode)
    {
        return new ModuleAllowance(moduleCode, isEnabled: false);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ModuleCode;
        yield return IsEnabled;
    }
}
