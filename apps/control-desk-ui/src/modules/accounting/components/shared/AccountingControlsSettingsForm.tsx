import { Save } from "lucide-react";
import type { FormEvent } from "react";
import type {
  AccountingControlSettingsInput,
  LedgerAccountSummary
} from "../../types/accountingTypes";
import {
  formatControlAccountContext,
  getControlAccountTone
} from "../../utils/accountingControlsWorkspaceModel";

type AccountingControlsSettingsFormProps = {
  value: AccountingControlSettingsInput;
  activePostingAccounts: LedgerAccountSummary[];
  equityAccounts: LedgerAccountSummary[];
  retainedEarningsAccount: LedgerAccountSummary | null;
  incomeSummaryAccount: LedgerAccountSummary | null;
  roundingAccount: LedgerAccountSummary | null;
  isBusy: boolean;
  canSave: boolean;
  onValueChange: (value: AccountingControlSettingsInput) => void;
  onSave: () => Promise<void>;
};

const accountingCompanyCode = "MAIN";

export function AccountingControlsSettingsForm({
  value,
  activePostingAccounts,
  equityAccounts,
  retainedEarningsAccount,
  incomeSummaryAccount,
  roundingAccount,
  isBusy,
  canSave,
  onValueChange,
  onSave
}: AccountingControlsSettingsFormProps) {
  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSave();
  }

  return (
    <section className="client-panel accounting-controls-settings-panel">
      <div className="client-panel-heading">
        <div>
          <span>{accountingCompanyCode}</span>
          <strong>GL control settings</strong>
        </div>
      </div>
      <form className="accounting-controls-form" onSubmit={handleSubmit}>
        <label className="form-field">
          <span>Company</span>
          <input
            value={accountingCompanyCode}
            disabled
            readOnly
          />
        </label>
        <label className="form-field">
          <span>Base currency</span>
          <input
            maxLength={3}
            value={value.baseCurrencyCode}
            onChange={(event) =>
              onValueChange({
                ...value,
                baseCurrencyCode: event.target.value.toUpperCase()
              })
            }
            disabled={isBusy}
          />
        </label>
        <AccountSelect
          label="Retained earnings"
          value={value.retainedEarningsAccountId}
          accounts={equityAccounts}
          selectedAccount={retainedEarningsAccount}
          expectedType="Equity"
          disabled={isBusy}
          onChange={(retainedEarningsAccountId) =>
            onValueChange({
              ...value,
              retainedEarningsAccountId
            })
          }
        />
        <AccountSelect
          label="Income summary"
          value={value.incomeSummaryAccountId}
          accounts={equityAccounts}
          selectedAccount={incomeSummaryAccount}
          expectedType="Equity"
          disabled={isBusy}
          onChange={(incomeSummaryAccountId) =>
            onValueChange({
              ...value,
              incomeSummaryAccountId
            })
          }
        />
        <AccountSelect
          label="Rounding"
          value={value.roundingAccountId}
          accounts={activePostingAccounts}
          selectedAccount={roundingAccount}
          disabled={isBusy}
          onChange={(roundingAccountId) =>
            onValueChange({
              ...value,
              roundingAccountId
            })
          }
        />
        <button
          className="icon-button primary"
          type="submit"
          disabled={isBusy || !canSave}
          title="Save GL controls"
        >
          <Save size={16} />
          Save
        </button>
      </form>
    </section>
  );
}

type AccountSelectProps = {
  label: string;
  value: string;
  accounts: LedgerAccountSummary[];
  selectedAccount?: LedgerAccountSummary | null;
  expectedType?: string;
  disabled: boolean;
  onChange: (value: string) => void;
};

function AccountSelect({
  label,
  value,
  accounts,
  selectedAccount,
  expectedType,
  disabled,
  onChange
}: AccountSelectProps) {
  const accountTone = getControlAccountTone(selectedAccount, expectedType);

  return (
    <label className="form-field">
      <span>{label}</span>
      <select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        disabled={disabled}
      >
        <option value="">Not set</option>
        {accounts.map((account) => (
          <option key={account.ledgerAccountId} value={account.ledgerAccountId}>
            {account.displayCode} {account.name}
          </option>
        ))}
      </select>
      <small className={`accounting-control-account-context ${accountTone}`}>
        {formatControlAccountContext(selectedAccount, expectedType)}
      </small>
    </label>
  );
}
