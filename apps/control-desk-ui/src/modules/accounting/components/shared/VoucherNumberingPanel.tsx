import { Hash, Save } from "lucide-react";
import type {
  VoucherNumberingRule,
  VoucherNumberingRuleInput
} from "../../types/accountingTypes";
import {
  formatSourceType,
  formatVoucherPattern,
  getVoucherNumberingFacts,
  getVoucherRuleState,
  isVoucherRuleFormValid,
  toVoucherRuleInput
} from "../../utils/accountingControlsWorkspaceModel";

type VoucherNumberingPanelProps = {
  voucherRules: VoucherNumberingRule[];
  voucherRuleForms: Record<string, VoucherNumberingRuleInput>;
  isBusy: boolean;
  onVoucherRuleChange: (sourceType: string, value: VoucherNumberingRuleInput) => void;
  onSaveVoucherRule: (sourceType: string) => Promise<void>;
};

export function VoucherNumberingPanel({
  voucherRules,
  voucherRuleForms,
  isBusy,
  onVoucherRuleChange,
  onSaveVoucherRule
}: VoucherNumberingPanelProps) {
  return (
    <section className="client-panel voucher-numbering-panel">
      <div className="voucher-numbering-heading">
        <div>
          <span>Voucher setup</span>
          <strong>{voucherRules.length} source types</strong>
        </div>
        <Hash size={17} />
      </div>
      <div className="voucher-numbering-status-row">
        {getVoucherNumberingFacts(voucherRules, voucherRuleForms).map((fact) => (
          <span className={fact.tone} key={fact.label} title={fact.title}>
            <small>{fact.label}</small>
            <strong>{fact.value}</strong>
          </span>
        ))}
      </div>
      <table className="voucher-numbering-table">
        <thead>
          <tr>
            <th>Source</th>
            <th>Pattern</th>
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
            const canSave = isVoucherRuleFormValid(form);
            const ruleState = getVoucherRuleState(rule, form);

            return (
              <tr className={ruleState.tone} key={rule.sourceType}>
                <td>{formatSourceType(rule.sourceType)}</td>
                <td>
                  <div className="voucher-pattern-cell">
                    <strong>{formatVoucherPattern(form)}</strong>
                    <small>{form.isActive ? "Next format" : "Disabled"}</small>
                  </div>
                </td>
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
                  <div className="voucher-rule-status-cell">
                    <span className={`voucher-rule-state ${ruleState.tone}`}>
                      {ruleState.label}
                    </span>
                    <small>{rule.isConfigured ? "Custom" : "Default"}</small>
                  </div>
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
    </section>
  );
}
