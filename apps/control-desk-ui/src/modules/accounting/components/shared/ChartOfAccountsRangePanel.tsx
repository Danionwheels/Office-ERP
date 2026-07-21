import { Save } from "lucide-react";
import type { FormEvent } from "react";
import {
  accountTypeOptions,
  normalBalanceOptions
} from "../../constants/accountingConstants";
import type {
  AccountCodeRange,
  AccountCodeRangeFormInput,
  AccountCodeRangeValidation,
  AccountCodeRangeValidationIssue
} from "../../types/accountingTypes";
import {
  formatRange,
  formatRangeLevelRule,
  formatRangeUsage,
  formatRangeValidationIssueMeta,
  getHighestIssueSeverity,
  getRangeEditorFacts,
  getRangeState,
  type CoaRangeFact,
  type CoaRangeIssueGroup
} from "../../utils/chartOfAccountsWorkspaceModel";

type ChartOfAccountsRangePanelProps = {
  activeRangeCount: number;
  canSaveRange: boolean;
  displayedRangeIssueGroups: CoaRangeIssueGroup[];
  displayedRangeValidationIssues: AccountCodeRangeValidationIssue[];
  filtersRole: string;
  isBusy: boolean;
  onRangeChange: (value: AccountCodeRangeFormInput) => void;
  onRangeSelect: (range: AccountCodeRange) => void;
  onSaveRange: () => Promise<void>;
  rangeSetupFacts: CoaRangeFact[];
  rangeUsageByRole: Map<string, number>;
  rangeValidation: AccountCodeRangeValidation | null;
  rangeValidationIssuesByRole: Map<string, AccountCodeRangeValidationIssue[]>;
  rangeValidationText: string;
  rangeValue: AccountCodeRangeFormInput;
  ranges: AccountCodeRange[];
  selectedRange: AccountCodeRange | null;
  selectedRangeFacts: CoaRangeFact[];
  selectedRangeIssues: AccountCodeRangeValidationIssue[];
  selectedRangeRole: string;
};

export function ChartOfAccountsRangePanel({
  activeRangeCount,
  canSaveRange,
  displayedRangeIssueGroups,
  displayedRangeValidationIssues,
  filtersRole,
  isBusy,
  onRangeChange,
  onRangeSelect,
  onSaveRange,
  rangeSetupFacts,
  rangeUsageByRole,
  rangeValidation,
  rangeValidationIssuesByRole,
  rangeValidationText,
  rangeValue,
  ranges,
  selectedRange,
  selectedRangeFacts,
  selectedRangeIssues,
  selectedRangeRole
}: ChartOfAccountsRangePanelProps) {
  async function handleSaveRange(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSaveRange();
  }

  return (
    <section className="entry-section coa-range-panel">
      <div className="section-heading-row">
        <h2>Account Ranges</h2>
        <span>{activeRangeCount} active ranges</span>
      </div>

      <div className="coa-range-readiness-row">
        {rangeSetupFacts.map((fact) => (
          <span className={fact.tone} key={fact.label} title={fact.title}>
            <small>{fact.label}</small>
            <strong>{fact.value}</strong>
          </span>
        ))}
      </div>

      {selectedRange !== null && (
        <div className="coa-range-rule-strip">
          {selectedRangeFacts.map((fact) => (
            <span className={fact.tone} key={fact.label} title={fact.title}>
              <small>{fact.label}</small>
              <strong>{fact.value}</strong>
            </span>
          ))}
        </div>
      )}

      {rangeValidation !== null && (
        <div className={`coa-range-validation${rangeValidation.issueCount === 0 ? " ok" : " attention"}`}>
          <div className="coa-range-validation-summary">
            <span>
              <small>Setup check</small>
              <strong>{rangeValidationText}</strong>
            </span>
            <span>
              <small>Ranges</small>
              <strong>{rangeValidation.activeRangeCount}/{rangeValidation.rangeCount}</strong>
            </span>
            <span>
              <small>Selected issues</small>
              <strong>{selectedRangeIssues.length}</strong>
            </span>
          </div>

          {displayedRangeIssueGroups.length > 0 && (
            <div className="coa-range-validation-groups">
              {displayedRangeIssueGroups.map((group) => (
                <span
                  className={group.tone}
                  key={group.code}
                  title={group.title}
                >
                  <small>{group.code}</small>
                  <strong>{group.count}</strong>
                </span>
              ))}
            </div>
          )}

          {displayedRangeValidationIssues.length === 0 ? (
            <span className="coa-range-validation-state">Setup clean</span>
          ) : (
            <div className="coa-range-validation-issues">
              {displayedRangeValidationIssues.slice(0, 8).map((issue, index) => {
                const severity = getHighestIssueSeverity([issue]);

                return (
                  <span
                    className={`coa-range-validation-issue ${severity}`}
                    key={`${issue.code}-${issue.rangeRole ?? "setup"}-${index}`}
                    title={formatRangeValidationIssueMeta(issue)}
                  >
                    <strong>{issue.code}</strong>
                    <small>{issue.message}</small>
                  </span>
                );
              })}
              {displayedRangeValidationIssues.length > 8 && (
                <span className="coa-range-validation-more">
                  +{displayedRangeValidationIssues.length - 8} more
                </span>
              )}
            </div>
          )}
        </div>
      )}

      <div className="coa-range-list">
        {ranges.length === 0 ? (
          <div className="client-empty-state">No ranges loaded</div>
        ) : (
          ranges.map((range) => {
            const rangeIssues = rangeValidationIssuesByRole.get(range.role) ?? [];
            const rangeIssueSeverity = getHighestIssueSeverity(rangeIssues);
            const usageCount = rangeUsageByRole.get(range.role) ?? 0;
            const rangeState = getRangeState(range, rangeIssues);

            return (
              <button
                className={`coa-range-item${
                  selectedRangeRole === range.role || filtersRole === range.role ? " active" : ""
                }${rangeIssues.length > 0 ? ` has-issues issue-${rangeIssueSeverity}` : ""}`}
                key={range.role}
                type="button"
                onClick={() => onRangeSelect(range)}
                disabled={isBusy}
              >
                <span className="coa-range-item-main">
                  <strong>{range.role}</strong>
                  <small>{range.displayName}</small>
                </span>
                <span className="coa-range-item-rule">
                  <em>{formatRange(range)}</em>
                  <small>{formatRangeLevelRule(range)}</small>
                </span>
                <span className={`coa-range-state ${rangeState.tone}`}>
                  {rangeState.label}
                </span>
                <small className="coa-range-usage">
                  {formatRangeUsage(range, usageCount)}
                </small>
                {rangeIssues.length > 0 && (
                  <small className={`coa-range-issue-badge ${rangeIssueSeverity}`}>
                    {rangeIssues.length} issue{rangeIssues.length === 1 ? "" : "s"}
                  </small>
                )}
              </button>
            );
          })
        )}
      </div>

      <form className="coa-range-form" onSubmit={handleSaveRange}>
        <div className="field-grid field-grid-three coa-range-fields">
          <label className="form-field">
            <span>Role</span>
            <input value={selectedRange?.role ?? ""} disabled={isBusy} readOnly />
          </label>
          <label className="form-field wide">
            <span>Name</span>
            <input
              value={rangeValue.displayName}
              onChange={(event) =>
                onRangeChange({
                  ...rangeValue,
                  displayName: event.target.value
                })
              }
              disabled={isBusy || selectedRange === null}
            />
          </label>
          <label className="form-field">
            <span>Prefix</span>
            <input
              value={rangeValue.searchPrefix}
              onChange={(event) =>
                onRangeChange({
                  ...rangeValue,
                  searchPrefix: event.target.value
                })
              }
              disabled={isBusy || selectedRange === null}
            />
          </label>
          <label className="form-field">
            <span>Start</span>
            <input
              value={rangeValue.rangeStart}
              onChange={(event) =>
                onRangeChange({
                  ...rangeValue,
                  rangeStart: event.target.value
                })
              }
              disabled={isBusy || selectedRange === null}
            />
          </label>
          <label className="form-field">
            <span>End</span>
            <input
              value={rangeValue.rangeEnd}
              onChange={(event) =>
                onRangeChange({
                  ...rangeValue,
                  rangeEnd: event.target.value
                })
              }
              disabled={isBusy || selectedRange === null}
            />
          </label>
          <label className="form-field">
            <span>Length</span>
            <input
              type="number"
              min="1"
              max="32"
              value={rangeValue.codeLength}
              onChange={(event) =>
                onRangeChange({
                  ...rangeValue,
                  codeLength: event.target.value
                })
              }
              disabled={isBusy || selectedRange === null}
            />
          </label>
          <label className="form-field">
            <span>Type</span>
            <select
              value={rangeValue.accountType}
              onChange={(event) =>
                onRangeChange({
                  ...rangeValue,
                  accountType: event.target.value
                })
              }
              disabled={isBusy || selectedRange === null}
            >
              {accountTypeOptions.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </label>
          <label className="form-field">
            <span>Balance</span>
            <select
              value={rangeValue.normalBalance}
              onChange={(event) =>
                onRangeChange({
                  ...rangeValue,
                  normalBalance: event.target.value
                })
              }
              disabled={isBusy || selectedRange === null}
            >
              {normalBalanceOptions.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </label>
          <label className="form-field">
            <span>Parent</span>
            <input
              value={rangeValue.parentCode}
              onChange={(event) =>
                onRangeChange({
                  ...rangeValue,
                  parentCode: event.target.value
                })
              }
              disabled={isBusy || selectedRange === null}
            />
          </label>
        </div>

        {selectedRange !== null && (
          <div className="coa-range-editor-facts">
            {getRangeEditorFacts(rangeValue, selectedRangeIssues).map((fact) => (
              <span className={fact.tone} key={fact.label} title={fact.title}>
                <small>{fact.label}</small>
                <strong>{fact.value}</strong>
              </span>
            ))}
          </div>
        )}

        <div className="coa-range-flags">
          <label>
            <input
              type="checkbox"
              checked={rangeValue.isPostingAccount}
              onChange={(event) =>
                onRangeChange({
                  ...rangeValue,
                  isPostingAccount: event.target.checked
                })
              }
              disabled={isBusy || selectedRange === null}
            />
            Posting
          </label>
          <label>
            <input
              type="checkbox"
              checked={rangeValue.isActive}
              onChange={(event) =>
                onRangeChange({
                  ...rangeValue,
                  isActive: event.target.checked
                })
              }
              disabled={isBusy || selectedRange === null}
            />
            Active
          </label>
        </div>

        <div className="section-actions coa-form-actions">
          <button
            className="icon-button primary"
            type="submit"
            disabled={isBusy || !canSaveRange}
            title="Save range rule"
          >
            <Save size={16} />
            Save range
          </button>
          {selectedRange !== null && (
            <span className="billing-small-fact">
              {formatRange(selectedRange)}
            </span>
          )}
        </div>
      </form>
    </section>
  );
}
