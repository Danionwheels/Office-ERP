using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Entitlements;

public sealed class EntitlementModule : ValueObject
{
    private EntitlementModule(ModuleCode moduleCode, bool isEnabled)
    {
        ModuleCode = moduleCode;
        IsEnabled = isEnabled;
    }

    public ModuleCode ModuleCode { get; }

    public bool IsEnabled { get; }

    public static EntitlementModule Create(ModuleCode moduleCode, bool isEnabled)
    {
        return new EntitlementModule(moduleCode, isEnabled);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ModuleCode;
        yield return IsEnabled;
    }
}
