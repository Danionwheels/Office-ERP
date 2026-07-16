using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Entitlements;

public sealed class ClientAccessRevisionModule : ValueObject
{
    private ClientAccessRevisionModule()
    {
        ModuleCode = null!;
    }

    private ClientAccessRevisionModule(ModuleCode moduleCode, bool isEnabled)
    {
        ModuleCode = moduleCode;
        IsEnabled = isEnabled;
    }

    public ModuleCode ModuleCode { get; private set; }

    public bool IsEnabled { get; private set; }

    public static ClientAccessRevisionModule Create(ModuleCode moduleCode, bool isEnabled)
    {
        return new ClientAccessRevisionModule(moduleCode, isEnabled);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ModuleCode;
        yield return IsEnabled;
    }
}
