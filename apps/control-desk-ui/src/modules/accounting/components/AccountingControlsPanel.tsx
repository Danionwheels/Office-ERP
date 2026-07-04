import { Hash, RefreshCw, Save, Settings2, WandSparkles } from "lucide-react";
import { type FormEvent } from "react";
import type {
  AccountingControlSettings,
  AccountingControlSettingsInput,
  LedgerAccountSummary,
  VoucherNumberingRule,
  VoucherNumberingRuleInput
} from "../types/accountingTypes";

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
            onClick={onUseDefaults}
            disabled={isBusy}
            title="Use default MAIN GL controls"
          >
            <WandSparkles size={16} />
            Use defaults
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

      <div className="voucher-numbering-panel">
        <div className="voucher-numbering-heading">
          <div>
            <span>Voucher setup</span>
            <strong>{voucherRules.length} source types</strong>
          </div>
          <Hash size={17} />
        </div>
        <table className="voucher-numbering-table">
          <thead>
            <tr>
              <th>Source</th>
              <th>Prefix</th>
              <th>Digits</th>
              <th>Active</th>
              <th>Status</th>
              <th>Updated</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody>
            {voucherRules.map((rule) => {
              const form = voucherRuleForms[rule.sourceType] ?? toVoucherRuleInput(rule);
              const canSave =
                form.prefix.trim() !== ""
                && Number(form.numberPaddingWidth) >= 1
                && Number(form.numberPaddingWidth) <= 10;

              return (
                <tr key={rule.sourceType}>
                  <td>{formatSourceType(rule.sourceType)}</td>
                  <td>
                    <input
                      value={form.prefix}
                      maxLength={16}
                      onChange={(event) =>
                        onVoucherRuleChange(rule.sourceType, {
                          ...form,
                          prefix: event.target.value.toUpperCase()
                        })
                      }
                      disabled={isBusy}
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      min={1}
                      max={10}
                      value={form.numberPaddingWidth}
                      onChange={(event) =>
                        onVoucherRuleChange(rule.sourceType, {
                          ...form,
                          numberPaddingWidth: event.target.value
                        })
                      }
                      disabled={isBusy}
                    />
                  </td>
                  <td>
                    <label className="inline-toggle">
                      <input
                        type="checkbox"
                        checked={form.isActive}
                        onChange={(event) =>
                          onVoucherRuleChange(rule.sourceType, {
                            ...form,
                            isActive: event.target.checked
                          })
                        }
                        disabled={isBusy}
                      />
                    </label>
                  </td>
                  <td>
                    <span className={`status-pill ${rule.isConfigured ? "open" : "draft"}`}>
                      {rule.isConfigured ? "Custom" : "Default"}
                    </span>
                  </td>
                  <td>{rule.updatedAtUtc ? rule.updatedAtUtc.slice(0, 10) : "-"}</td>
                  <td>
                    <button
                      className="table-icon-button"
                      type="button"
                      onClick={() => void onSaveVoucherRule(rule.sourceType)}
                      disabled={isBusy || !canSave}
                      title={`Save ${formatSourceType(rule.sourceType)} voucher numbering`}
                    >
                      <Save size={14} />
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
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

function toVoucherRuleInput(rule: VoucherNumberingRule): VoucherNumberingRuleInput {
  return {
    prefix: rule.prefix,
    numberPaddingWidth: rule.numberPaddingWidth.toString(),
    isActive: rule.isActive
  };
}

function formatSourceType(sourceType: string): string {
  return sourceType.replace(/([a-z])([A-Z])/g, "$1 $2");
}
