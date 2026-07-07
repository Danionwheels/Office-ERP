import {
  Save,
  Wand2,
  X
} from "lucide-react";
import type { FormEvent } from "react";
import type { ApiErrorItem } from "../../../../shared/api/apiError";
import {
  accountTypeOptions,
  accountingCurrencyCode,
  legacyAccountCreateLevels,
  normalBalanceOptions,
  type LegacyAccountLevel
} from "../../constants/accountingConstants";
import type {
  AccountCodeRange,
  LedgerAccountEditorInput
} from "../../types/accountingTypes";
import {
  getPersistedLegacyAccountLevel,
  isPostingLedgerAccountLevel
} from "../../utils/chartOfAccountsModel";
import {
  formatAccountSaveError,
  type CoaInlineCreateStatusItem
} from "../../utils/chartOfAccountsWorkspaceModel";

type ChartOfAccountsDetailsPanelProps = {
  accountFormId: string;
  accountLevelOptions: LegacyAccountLevel[];
  accountMode: "create" | "edit";
  accountSaveErrors: ApiErrorItem[];
  accountValue: LedgerAccountEditorInput;
  canSaveAccount: boolean;
  inlineCodePlaceholder: string;
  inlineCreateParentDisplay: string;
  inlineCreateReady: boolean;
  inlineCreateStatusItems: CoaInlineCreateStatusItem[];
  inlineNamePlaceholder: string;
  isBusy: boolean;
  isInlineCreating: boolean;
  onAccountChange: (value: LedgerAccountEditorInput) => void;
  onCancelCreate: () => void;
  onSaveAccount: () => Promise<void>;
  onSuggestAccountCode: () => Promise<void>;
  parentAccountDisplay: string;
  selectedRange: AccountCodeRange | null;
};

export function ChartOfAccountsDetailsPanel({
  accountFormId,
  accountLevelOptions,
  accountMode,
  accountSaveErrors,
  accountValue,
  canSaveAccount,
  inlineCodePlaceholder,
  inlineCreateParentDisplay,
  inlineCreateReady,
  inlineCreateStatusItems,
  inlineNamePlaceholder,
  isBusy,
  isInlineCreating,
  onAccountChange,
  onCancelCreate,
  onSaveAccount,
  onSuggestAccountCode,
  parentAccountDisplay,
  selectedRange
}: ChartOfAccountsDetailsPanelProps) {
  const allowedCreateLevelCodes = new Set(accountLevelOptions.map((level) => level.code));
  const persistedCreateLevel = getPersistedLegacyAccountLevel(accountValue.level);
  const createLevelValue =
    persistedCreateLevel !== null && persistedCreateLevel.code !== "D"
      ? persistedCreateLevel.label
      : "";

  async function handleSaveAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSaveAccount();
  }

  return (
    <section className="entry-section coa-account-panel">
      <div className="section-heading-row">
        <h2>Account Details</h2>
        <span>
          {accountMode === "edit"
            ? "Edit ledger account"
            : isInlineCreating
              ? "New account entry"
              : "No account selected"}
        </span>
      </div>

      {accountMode === "edit" && (
        <form
          className="coa-account-form"
          id={accountFormId}
          onSubmit={handleSaveAccount}
        >
          <div className="field-grid field-grid-three coa-account-fields">
            <label className="form-field">
              <span>Acc Code</span>
              <input
                value={accountValue.code}
                onChange={(event) =>
                  onAccountChange({
                    ...accountValue,
                    code: event.target.value
                  })
                }
                disabled={isBusy || accountMode === "edit"}
              />
            </label>
            <label className="form-field wide">
              <span>Account Name</span>
              <input
                value={accountValue.name}
                onChange={(event) =>
                  onAccountChange({
                    ...accountValue,
                    name: event.target.value
                  })
                }
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Class</span>
              <select
                value={accountValue.type}
                onChange={(event) =>
                  onAccountChange({
                    ...accountValue,
                    type: event.target.value
                  })
                }
                disabled={isBusy || accountMode === "edit"}
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
                value={accountValue.normalBalance}
                onChange={(event) =>
                  onAccountChange({
                    ...accountValue,
                    normalBalance: event.target.value
                  })
                }
                disabled={isBusy || accountMode === "edit"}
              >
                {normalBalanceOptions.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </label>
            <label className="form-field">
              <span>Acc Type</span>
              <select
                value={accountValue.level}
                onChange={(event) => {
                  const level = event.target.value;

                  onAccountChange({
                    ...accountValue,
                    level,
                    isPostingAccount: isPostingLedgerAccountLevel(level)
                  });
                }}
                disabled={isBusy || accountMode === "edit"}
              >
                {accountLevelOptions.map((level) => (
                  <option key={level.code} value={level.label}>
                    {level.code} {level.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="form-field">
              <span>Parent</span>
              <input
                value={parentAccountDisplay}
                disabled={isBusy}
                readOnly
              />
            </label>
            <label className="form-field">
              <span>Status</span>
              <select
                value={accountValue.status}
                onChange={(event) =>
                  onAccountChange({
                    ...accountValue,
                    status: event.target.value
                  })
                }
                disabled={isBusy}
              >
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
              </select>
            </label>
            <label className="checkbox-field coa-checkbox-field">
              <input
                type="checkbox"
                checked={accountValue.isPostingAccount}
                onChange={(event) =>
                  onAccountChange({
                    ...accountValue,
                    isPostingAccount: event.target.checked
                  })
                }
                disabled
              />
              Posting account
            </label>
          </div>
          {renderAccountSaveErrors(accountSaveErrors)}
        </form>
      )}

      {accountMode === "create" && !isInlineCreating && (
        <div className={`coa-account-create-summary${isInlineCreating ? " active" : ""}`}>
          <span className={isInlineCreating ? "ready" : "neutral"}>
            <small>Mode</small>
            <strong>{isInlineCreating ? "Create" : "Waiting"}</strong>
          </span>
          <span>
            <small>Range</small>
            <strong>{selectedRange?.displayName ?? "All ranges"}</strong>
          </span>
          <span>
            <small>Parent</small>
            <strong>{inlineCreateParentDisplay}</strong>
          </span>
          <span>
            <small>Level</small>
            <strong>{accountValue.level.trim() === "" ? "Not set" : accountValue.level}</strong>
          </span>
          <span className={inlineCreateReady ? "ready" : "warning"}>
            <small>Status</small>
            <strong>{inlineCreateReady ? "Ready" : "Incomplete"}</strong>
          </span>
        </div>
      )}

      {accountMode === "create" && isInlineCreating && (
        <form
          className="coa-account-form coa-account-create-form"
          id={accountFormId}
          onSubmit={handleSaveAccount}
        >
          <div
            className="coa-inline-create-status-row coa-account-create-status-row"
            aria-label="Account create readiness"
            aria-live="polite"
          >
            {inlineCreateStatusItems.map((item) => (
              <span className={item.tone} key={item.label} title={item.title}>
                <small>{item.label}</small>
                <strong>{item.value}</strong>
              </span>
            ))}
          </div>

          <div className="field-grid field-grid-three coa-account-fields">
            <label className="form-field">
              <span>Acc Code</span>
              <input
                value={accountValue.code}
                placeholder={inlineCodePlaceholder}
                aria-required="true"
                aria-readonly="true"
                readOnly
                title="Ledger account code is generated automatically"
                disabled={isBusy}
              />
            </label>
            <label className="form-field wide">
              <span>Account Name</span>
              <input
                value={accountValue.name}
                placeholder={inlineNamePlaceholder}
                aria-required="true"
                onChange={(event) =>
                  onAccountChange({
                    ...accountValue,
                    name: event.target.value
                  })
                }
                disabled={isBusy}
                autoFocus
              />
            </label>
            <label className="form-field">
              <span>Acc Type</span>
              <select
                value={createLevelValue}
                aria-required="true"
                onChange={(event) => {
                  const level = event.target.value;

                  onAccountChange({
                    ...accountValue,
                    level,
                    isPostingAccount: isPostingLedgerAccountLevel(level)
                  });
                }}
                disabled={isBusy}
              >
                <option value="" disabled>
                  Select type
                </option>
                {legacyAccountCreateLevels.map((level) => {
                  const isAllowed = allowedCreateLevelCodes.has(level.code);

                  return (
                    <option
                      key={level.code}
                      value={level.label}
                      disabled={!isAllowed}
                    >
                      {level.label}
                    </option>
                  );
                })}
              </select>
            </label>
            <label className="form-field">
              <span>Class</span>
              <select value={accountValue.type} disabled>
                {accountTypeOptions.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </label>
            <label className="form-field">
              <span>Balance</span>
              <select value={accountValue.normalBalance} disabled>
                {normalBalanceOptions.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </label>
            <label className="form-field">
              <span>Currency</span>
              <input value={accountingCurrencyCode} readOnly disabled={isBusy} />
            </label>
            <label className="form-field wide">
              <span>Parent</span>
              <input
                value={inlineCreateParentDisplay}
                readOnly
                disabled={isBusy}
              />
            </label>
            <label className="form-field">
              <span>Status</span>
              <select
                value={accountValue.status}
                onChange={(event) =>
                  onAccountChange({
                    ...accountValue,
                    status: event.target.value
                  })
                }
                disabled={isBusy}
              >
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
              </select>
            </label>
            <label className="checkbox-field coa-checkbox-field">
              <input
                type="checkbox"
                checked={accountValue.isPostingAccount}
                disabled
              />
              Posting account
            </label>
          </div>

          {renderAccountSaveErrors(accountSaveErrors)}

          <div className="section-actions coa-form-actions">
            <button
              className="icon-button primary"
              type="submit"
              disabled={isBusy || !canSaveAccount}
              title="Create ledger account"
            >
              <Save size={15} />
              Save
            </button>
            <button
              className="icon-button"
              type="button"
              onClick={() => void onSuggestAccountCode()}
              disabled={isBusy || selectedRange === null}
              title="Regenerate automatic account code"
            >
              <Wand2 size={15} />
              Code
            </button>
            <button
              className="icon-button"
              type="button"
              onClick={onCancelCreate}
              disabled={isBusy}
              title="Cancel account entry"
            >
              <X size={15} />
              Cancel
            </button>
          </div>
        </form>
      )}
    </section>
  );
}

function renderAccountSaveErrors(accountSaveErrors: ApiErrorItem[]) {
  if (accountSaveErrors.length === 0) {
    return null;
  }

  return (
    <div className="coa-account-save-errors" role="alert">
      {accountSaveErrors.map((error, index) => (
        <span key={`${error.target ?? "account"}-${error.code}-${index}`}>
          {formatAccountSaveError(error)}
        </span>
      ))}
    </div>
  );
}
