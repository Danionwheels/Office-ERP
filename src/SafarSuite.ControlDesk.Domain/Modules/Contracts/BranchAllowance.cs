using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public sealed class BranchAllowance : ValueObject
{
    private BranchAllowance(int allowedBranches)
    {
        AllowedBranches = allowedBranches;
    }

    public int AllowedBranches { get; }

    public static BranchAllowance Create(int allowedBranches)
    {
        if (allowedBranches < 0)
        {
            throw new ArgumentException("Allowed branch count cannot be negative.", nameof(allowedBranches));
        }

        return new BranchAllowance(allowedBranches);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AllowedBranches;
    }
}
