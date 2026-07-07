import { X } from "lucide-react";
import type {
  AccountingControlSettingsInput,
  LedgerAccountSummary,
  VoucherNumberingRule,
  VoucherNumberingRuleInput
} from "../../types/accountingTypes";
import { AccountingControlsSettingsForm } from "./AccountingControlsSettingsForm";
import { VoucherNumberingPanel } from "./VoucherNumberingPanel";

type AccountingControlsWorkWindowProps = {
  value: AccountingControlSettingsInput;
  activePostingAccounts: LedgerAccountSummary[];
  equityAccounts: LedgerAccountSummary[];
  retainedEarningsAccount: LedgerAccountSummary | null;
  incomeSummaryAccount: LedgerAccountSummary | null;
  roundingAccount: LedgerAccountSummary | null;
  voucherRules: VoucherNumberingRule[];
  voucherRuleForms: Record<string, VoucherNumberingRuleInput>;
  isBusy: boolean;
  canSave: boolean;
  onValueChange: (value: AccountingControlSettingsInput) => void;
  onVoucherRuleChange: (sourceType: string, value: VoucherNumberingRuleInput) => void;
  onSaveVoucherRule: (sourceType: string) => Promise<void>;
  onSave: () => Promise<void>;
  onClose: () => void;
};

export function AccountingControlsWorkWindow({
  value,
  activePostingAccounts,
  equityAccounts,
  retainedEarningsAccount,
  incomeSummaryAccount,
  roundingAccount,
  voucherRules,
  voucherRuleForms,
  isBusy,
  canSave,
  onValueChange,
  onVoucherRuleChange,
  onSaveVoucherRule,
  onSave,
  onClose
}: AccountingControlsWorkWindowProps) {
  return (
    <aside
      className="accounting-controls-work-window"
      role="dialog"
      aria-label="Accounting controls work window"
    >
      <header className="accounting-controls-work-window-header">
        <div>
          <strong>Controls Work Window</strong>
          <small>GL settings and voucher numbering</small>
        </div>
        <button
          className="table-icon-button"
          type="button"
          onClick={onClose}
          title="Close controls work window"
        >
          <X size={15} />
        </button>
      </header>
      <div className="accounting-controls-work-window-body">
        <AccountingControlsSettingsForm
          value={value}
          activePostingAccounts={activePostingAccounts}
          equityAccounts={equityAccounts}
          retainedEarningsAccount={retainedEarningsAccount}
          incomeSummaryAccount={incomeSummaryAccount}
          roundingAccount={roundingAccount}
          isBusy={isBusy}
          canSave={canSave}
          onValueChange={onValueChange}
          onSave={onSave}
        />

        <VoucherNumberingPanel
          voucherRules={voucherRules}
          voucherRuleForms={voucherRuleForms}
          isBusy={isBusy}
          onVoucherRuleChange={onVoucherRuleChange}
          onSaveVoucherRule={onSaveVoucherRule}
        />
      </div>
    </aside>
  );
}
