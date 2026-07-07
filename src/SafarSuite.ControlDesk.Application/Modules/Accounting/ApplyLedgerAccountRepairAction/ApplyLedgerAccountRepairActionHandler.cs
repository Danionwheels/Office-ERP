using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Accounting.GetLedgerAccountRepairPlan;
using SafarSuite.ControlDesk.Application.Modules.Accounting.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Application.Modules.Accounting.ApplyLedgerAccountRepairAction;

public sealed class ApplyLedgerAccountRepairActionHandler
{
    private const string PostingFlagActionCode = "UpdatePostingFlag";
    private const string PostingFlagRepairMode = "GuidedPostingFlagUpdate";

    private readonly GetLedgerAccountRepairPlanHandler _repairPlan;
    private readonly ILedgerAccountRepository _ledgerAccounts;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyLedgerAccountRepairActionHandler(
        GetLedgerAccountRepairPlanHandler repairPlan,
        ILedgerAccountRepository ledgerAccounts,
        IUnitOfWork unitOfWork)
    {
        _repairPlan = repairPlan;
        _ledgerAccounts = ledgerAccounts;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ApplyLedgerAccountRepairActionResult>> HandleAsync(
        ApplyLedgerAccountRepairActionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.IssueCode))
        {
            return Result<ApplyLedgerAccountRepairActionResult>.Failure(ApplicationError.Validation(
                nameof(command.IssueCode),
                "Repair issue code is required."));
        }

        if (string.IsNullOrWhiteSpace(command.ActionCode))
        {
            return Result<ApplyLedgerAccountRepairActionResult>.Failure(ApplicationError.Validation(
                nameof(command.ActionCode),
                "Repair action code is required."));
        }

        if (!command.Confirmed)
        {
            return Result<ApplyLedgerAccountRepairActionResult>.Failure(ApplicationError.Validation(
                nameof(command.Confirmed),
                "Repair action confirmation is required."));
        }

        var plan = await _repairPlan.HandleAsync(
            new GetLedgerAccountRepairPlanQuery(command.CompanyCode),
            cancellationToken);

        if (plan.IsFailure)
        {
            return Result<ApplyLedgerAccountRepairActionResult>.Failure(plan.Errors);
        }

        var planItem = plan.Value.Items.SingleOrDefault(item => item.LedgerAccountId == command.LedgerAccountId);

        if (planItem is null)
        {
            return Result<ApplyLedgerAccountRepairActionResult>.Failure(ApplicationError.Conflict(
                nameof(command.LedgerAccountId),
                "Ledger account no longer has repair-plan actions."));
        }

        var action = planItem.Actions.SingleOrDefault(candidate =>
            string.Equals(candidate.IssueCode, command.IssueCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidate.ActionCode, command.ActionCode, StringComparison.OrdinalIgnoreCase));

        if (action is null)
        {
            return Result<ApplyLedgerAccountRepairActionResult>.Failure(ApplicationError.Conflict(
                nameof(command.ActionCode),
                "Requested repair action is not available in the current repair plan."));
        }

        if (!action.IsAutomatable)
        {
            return Result<ApplyLedgerAccountRepairActionResult>.Failure(ApplicationError.Conflict(
                nameof(command.ActionCode),
                "Requested repair action requires manual review."));
        }

        if (!string.Equals(action.ActionCode, PostingFlagActionCode, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(action.RepairMode, PostingFlagRepairMode, StringComparison.OrdinalIgnoreCase))
        {
            return Result<ApplyLedgerAccountRepairActionResult>.Failure(ApplicationError.Conflict(
                nameof(command.ActionCode),
                "Requested repair action is not supported for guided application yet."));
        }

        if (!TryParsePostingFlag(action.SuggestedValue, out var desiredPostingFlag))
        {
            return Result<ApplyLedgerAccountRepairActionResult>.Failure(ApplicationError.Conflict(
                nameof(action.SuggestedValue),
                "Repair action does not provide a posting flag target."));
        }

        var account = await _ledgerAccounts.GetByIdAsync(
            LedgerAccountId.Create(command.LedgerAccountId),
            cancellationToken);

        if (account is null)
        {
            return Result<ApplyLedgerAccountRepairActionResult>.Failure(ApplicationError.NotFound(
                nameof(command.LedgerAccountId),
                "Ledger account was not found."));
        }

        try
        {
            account.SetPostingAccount(desiredPostingFlag);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ApplyLedgerAccountRepairActionResult>.Success(new ApplyLedgerAccountRepairActionResult(
                account.Id.Value,
                account.Code.Value,
                account.Name,
                account.Type.ToString(),
                account.NormalBalance.ToString(),
                account.Level.ToString(),
                account.ParentAccountId?.Value,
                account.IsPostingAccount,
                account.Status.ToString(),
                account.CreatedAtUtc,
                action));
        }
        catch (ArgumentException exception)
        {
            return Result<ApplyLedgerAccountRepairActionResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command.ActionCode),
                exception.Message));
        }
    }

    private static bool TryParsePostingFlag(string? value, out bool isPostingAccount)
    {
        if (string.Equals(value, "Posting", StringComparison.OrdinalIgnoreCase))
        {
            isPostingAccount = true;

            return true;
        }

        if (string.Equals(value, "Non-posting", StringComparison.OrdinalIgnoreCase))
        {
            isPostingAccount = false;

            return true;
        }

        isPostingAccount = false;

        return false;
    }
}
