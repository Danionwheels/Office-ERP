import {
  PanelRightOpen,
  RefreshCw,
  WandSparkles
} from "lucide-react";
import { useState } from "react";
import type {
  AccountingControlSettings,
  AccountingControlSettingsInput,
  LedgerAccountSummary,
  VoucherNumberingRule,
  VoucherNumberingRuleInput
} from "../types/accountingTypes";
import {
  canSaveAccountingControls,
  findAccountById,
  getAccountingControlReadinessFacts,
  getAccountingControlSummaryFacts,
  getActivePostingAccounts,
  getEquityPostingAccounts,
  getVoucherNumberingFacts
} from "../utils/accountingControlsWorkspaceModel";
import { AccountingControlsReadinessStrip } from "./shared/AccountingControlsReadinessStrip";
import { AccountingControlsSummaryPanel } from "./shared/AccountingControlsSummaryPanel";
import { AccountingControlsWorkWindow } from "./shared/AccountingControlsWorkWindow";

type AccountingControlsPanelProps = {
  settings: AccountingControlSettings | null;
  value: AccountingControlSettingsInput;
  voucherRules: VoucherNumberingRule[];
  voucherRuleForms: Record<string, VoucherNumberingRuleInput>;
  accounts: LedgerAccountSummary[];
  isBusy: boolean;
  onValueChange: (value: AccountingControlSettingsInput) => void;
  onVoucherRuleChange: (sourceType: string, value: VoucherNumberingRuleInput) => void;
  onSaveVoucherRule: (sourceType: string) => Promise<void>;
  onSave: () => Promise<void>;
  onUseDefaults: () => Promise<void>;
  onRefresh: () => Promise<void>;
};

const accountingCompanyCode = "MAIN";

export function AccountingControlsPanel({
  settings,
  value,
  voucherRules,
  voucherRuleForms,
  accounts,
  isBusy,
  onValueChange,
  onVoucherRuleChange,
  onSaveVoucherRule,
  onSave,
  onUseDefaults,
  onRefresh
}: AccountingControlsPanelProps) {
  const [isWorkWindowOpen, setIsWorkWindowOpen] = useState(false);
  const activePostingAccounts = getActivePostingAccounts(accounts);
  const equityAccounts = getEquityPostingAccounts(activePostingAccounts);
  const retainedEarningsAccount = findAccountById(accounts, value.retainedEarningsAccountId);
  const incomeSummaryAccount = findAccountById(accounts, value.incomeSummaryAccountId);
  const roundingAccount = findAccountById(accounts, value.roundingAccountId);
  const controlReadinessFacts = getAccountingControlReadinessFacts({
    settings,
    value,
    activePostingAccountCount: activePostingAccounts.length,
    retainedEarningsAccount,
    incomeSummaryAccount,
    roundingAccount,
    voucherRules,
    voucherRuleForms
  });
  const controlSummaryFacts = getAccountingControlSummaryFacts(settings);
  const voucherFacts = getVoucherNumberingFacts(voucherRules, voucherRuleForms);
  const canSave = canSaveAccountingControls(value);

  async function handleUseDefaults() {
    setIsWorkWindowOpen(true);
    await onUseDefaults();
  }

  return (
    <section className="accounting-controls-panel">
      <header className="client-panel accounting-controls-header">
        <div>
          <span>{accountingCompanyCode}</span>
          <h2>GL Controls</h2>
        </div>
        <div className="accounting-controls-actions">
          <span className={`status-pill ${settings?.isConfigured ? "open" : "draft"}`}>
            {settings?.isConfigured ? "Configured" : "Partial"}
          </span>
          <button
            className={`icon-button${isWorkWindowOpen ? " primary" : ""}`}
            type="button"
            onClick={() => setIsWorkWindowOpen(true)}
            disabled={isBusy}
            title="Open controls work window"
          >
            <PanelRightOpen size={16} />
            Window
          </button>
          <button
            className="icon-button"
            type="button"
            onClick={() => void handleUseDefaults()}
            disabled={isBusy}
            title="Use default MAIN GL controls"
          >
            <WandSparkles size={16} />
            Defaults
          </button>
          <button
            className="icon-button"
            type="button"
            onClick={onRefresh}
            disabled={isBusy}
            title="Refresh GL controls"
          >
            <RefreshCw size={16} />
            Refresh
          </button>
        </div>
      </header>

      <AccountingControlsReadinessStrip facts={controlReadinessFacts} />

      <AccountingControlsSummaryPanel
        controlFacts={controlSummaryFacts}
        voucherFacts={voucherFacts}
      />

      {isWorkWindowOpen && (
        <AccountingControlsWorkWindow
          value={value}
          activePostingAccounts={activePostingAccounts}
          equityAccounts={equityAccounts}
          retainedEarningsAccount={retainedEarningsAccount}
          incomeSummaryAccount={incomeSummaryAccount}
          roundingAccount={roundingAccount}
          voucherRules={voucherRules}
          voucherRuleForms={voucherRuleForms}
          isBusy={isBusy}
          canSave={canSave}
          onValueChange={onValueChange}
          onVoucherRuleChange={onVoucherRuleChange}
          onSaveVoucherRule={onSaveVoucherRule}
          onSave={onSave}
          onClose={() => setIsWorkWindowOpen(false)}
        />
      )}
    </section>
  );
}
