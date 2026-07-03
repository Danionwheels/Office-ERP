import { RefreshCw, Save, Settings2 } from "lucide-react";
import { type FormEvent } from "react";
import type {
  AccountingControlSettings,
  AccountingControlSettingsInput,
  LedgerAccountSummary
} from "../types/accountingTypes";

type AccountingControlsPanelProps = {
  settings: AccountingControlSettings | null;
  value: AccountingControlSettingsInput;
  accounts: LedgerAccountSummary[];
  isBusy: boolean;
  onValueChange: (value: AccountingControlSettingsInput) => void;
  onSave: () => Promise<void>;
  onRefresh: () => Promise<void>;
};

const accountingCompanyCode = "MAIN";

export function AccountingControlsPanel({
  settings,
  value,
  accounts,
  isBusy,
  onValueChange,
  onSave,
  onRefresh
}: AccountingControlsPanelProps) {
  const activePostingAccounts = accounts
    .filter((account) => account.status === "Active" && account.isPostingAccount)
    .sort((left, right) => left.code.localeCompare(right.code));
  const equityAccounts = activePostingAccounts.filter((account) => account.type === "Equity");
  const canSave =
    value.baseCurrencyCode.trim().length === 3
    && hasDistinctSelectedAccounts(value);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSave();
  }

  return (
    <section className="client-panel accounting-controls-panel">
      <div className="client-panel-heading">
        <div>
          <span>{accountingCompanyCode}</span>
          <strong>GL controls</strong>
        </div>
        <div className="accounting-controls-actions">
          <span className={`status-pill ${settings?.isConfigured ? "open" : "draft"}`}>
            {settings?.isConfigured ? "Configured" : "Partial"}
          </span>
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

      <div className="accounting-controls-summary">
        <ControlFact
          label="Retained earnings"
          value={formatControlAccount(settings?.retainedEarningsAccount)}
        />
        <ControlFact
          label="Income summary"
          value={formatControlAccount(settings?.incomeSummaryAccount)}
        />
        <ControlFact
          label="Rounding"
          value={formatControlAccount(settings?.roundingAccount)}
        />
        <ControlFact
          label="Updated"
          value={settings?.updatedAtUtc ? settings.updatedAtUtc.slice(0, 10) : "-"}
        />
      </div>
    </section>
  );
}

type AccountSelectProps = {
  label: string;
  value: string;
  accounts: LedgerAccountSummary[];
  disabled: boolean;
  onChange: (value: string) => void;
};

function AccountSelect({
  label,
  value,
  accounts,
  disabled,
  onChange
}: AccountSelectProps) {
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
    </label>
  );
}

function ControlFact({
  label,
  value
}: {
  label: string;
  value: string;
}) {
  return (
    <span>
      <Settings2 size={15} />
      <small>{label}</small>
      <strong>{value}</strong>
    </span>
  );
}

function formatControlAccount(
  account: AccountingControlSettings["retainedEarningsAccount"]
): string {
  return account === null || account === undefined
    ? "-"
    : `${account.code} ${account.name}`;
}

function hasDistinctSelectedAccounts(value: AccountingControlSettingsInput): boolean {
  const selected = [
    value.retainedEarningsAccountId,
    value.incomeSummaryAccountId,
    value.roundingAccountId
  ].filter((accountId) => accountId.trim() !== "");

  return new Set(selected).size === selected.length;
}
