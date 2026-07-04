import {
  BookOpen,
  ListTree,
  Pencil,
  Plus,
  Power,
  RefreshCw,
  RotateCcw,
  Save,
  Search,
  SlidersHorizontal,
  Wand2
} from "lucide-react";
import { type FormEvent } from "react";
import {
  accountTypeOptions,
  legacyAccountLevels,
  normalBalanceOptions
} from "../constants/accountingConstants";
import type {
  AccountCodeRange,
  AccountCodeRangeFormInput,
  LedgerAccountActivity,
  LedgerAccountActivityLine,
  LedgerAccountEditorInput,
  LedgerAccountFilters,
  JournalEntrySummary,
  LedgerAccountSummary
} from "../types/accountingTypes";
import {
  getLedgerAccountLevelOptions,
  getLegacyAccountLevel,
  getVisibleAccounts,
  isPostingLedgerAccountLevel
} from "../utils/chartOfAccountsModel";
import { formatMoney } from "../utils/journalModel";

type ChartOfAccountsPanelProps = {
  accounts: LedgerAccountSummary[];
  ranges: AccountCodeRange[];
  filters: LedgerAccountFilters;
  selectedRangeRole: string;
  rangeValue: AccountCodeRangeFormInput;
  accountMode: "create" | "edit";
  accountValue: LedgerAccountEditorInput;
  activity: LedgerAccountActivity | null;
  journalEntries: JournalEntrySummary[];
  isBusy: boolean;
  onFiltersChange: (value: LedgerAccountFilters) => void;
  onRangeSelect: (range: AccountCodeRange) => void;
  onRangeChange: (value: AccountCodeRangeFormInput) => void;
  onSaveRange: () => Promise<void>;
  onAccountChange: (value: LedgerAccountEditorInput) => void;
  onNewAccount: () => Promise<void>;
  onEditAccount: (account: LedgerAccountSummary) => void;
  onSaveAccount: () => Promise<void>;
  onToggleAccountStatus: (account: LedgerAccountSummary) => Promise<void>;
  onViewAccountActivity: (account: LedgerAccountSummary) => Promise<void>;
  onViewJournalEntry: (line: LedgerAccountActivityLine) => Promise<void>;
  onSuggestAccountCode: () => Promise<void>;
  onRefresh: () => Promise<void>;
};

export function ChartOfAccountsPanel({
  accounts,
  ranges,
  filters,
  selectedRangeRole,
  rangeValue,
  accountMode,
  accountValue,
  activity,
  journalEntries,
  isBusy,
  onFiltersChange,
  onRangeSelect,
  onRangeChange,
  onSaveRange,
  onAccountChange,
  onNewAccount,
  onEditAccount,
  onSaveAccount,
  onToggleAccountStatus,
  onViewAccountActivity,
  onViewJournalEntry,
  onSuggestAccountCode,
  onRefresh
}: ChartOfAccountsPanelProps) {
  const activeRanges = ranges.filter((range) => range.isActive);
  const selectedRange = ranges.find((range) => range.role === selectedRangeRole) ?? null;
  const accountLevelOptions = getLedgerAccountLevelOptions(
    selectedRange,
    accountMode,
    accountValue.level
  );
  const visibleAccounts = getVisibleAccounts(accounts, ranges, filters);
  const postingCount = visibleAccounts.filter((account) => account.isPostingAccount).length;
  const nonPostingCount = visibleAccounts.length - postingCount;
  const canSaveRange =
    selectedRange !== null
    && rangeValue.displayName.trim() !== ""
    && rangeValue.searchPrefix.trim() !== ""
    && rangeValue.rangeStart.trim() !== ""
    && rangeValue.rangeEnd.trim() !== ""
    && Number(rangeValue.codeLength) > 0
    && rangeValue.accountType.trim() !== ""
    && rangeValue.normalBalance.trim() !== "";
  const canSuggestAccountCode = accountMode === "create" && selectedRangeRole !== "";
  const canSaveAccount =
    accountValue.name.trim() !== ""
    && accountValue.status.trim() !== ""
    && (accountMode === "edit"
      || (accountValue.code.trim() !== ""
        && accountValue.type.trim() !== ""
        && accountValue.normalBalance.trim() !== ""));

  async function handleSaveRange(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSaveRange();
  }

  async function handleSaveAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSaveAccount();
  }

  function handleSelectRange(range: AccountCodeRange) {
    onRangeSelect(range);
    onFiltersChange({
      ...filters,
      role: filters.role === range.role ? "" : range.role
    });
  }

  return (
    <section className="coa-workspace">
      <header className="coa-header client-panel">
        <div>
          <span>{filters.companyCode.trim() === "" ? "MAIN" : filters.companyCode}</span>
          <h2>Chart of accounts</h2>
        </div>
        <button
          className="icon-button"
          type="button"
          onClick={onRefresh}
          disabled={isBusy}
          title="Refresh chart of accounts"
        >
          <RefreshCw size={16} />
          Refresh
        </button>
      </header>

      <div className="coa-filter-panel client-panel">
        <label className="form-field">
          <span>Search</span>
          <span className="coa-search-input">
            <Search size={15} />
            <input
              value={filters.search}
              onChange={(event) =>
                onFiltersChange({
                  ...filters,
                  search: event.target.value
                })
              }
              disabled={isBusy}
            />
          </span>
        </label>
        <label className="form-field">
          <span>Type</span>
          <select
            value={filters.type}
            onChange={(event) =>
              onFiltersChange({
                ...filters,
                type: event.target.value
              })
            }
            disabled={isBusy}
          >
            <option value="">All</option>
            <option value="Asset">Asset</option>
            <option value="Liability">Liability</option>
            <option value="Equity">Equity</option>
            <option value="Revenue">Revenue</option>
            <option value="Expense">Expense</option>
          </select>
        </label>
        <label className="form-field">
          <span>Status</span>
          <select
            value={filters.status}
            onChange={(event) =>
              onFiltersChange({
                ...filters,
                status: event.target.value
              })
            }
            disabled={isBusy}
          >
            <option value="">All</option>
            <option value="Active">Active</option>
            <option value="Inactive">Inactive</option>
          </select>
        </label>
        <label className="form-field">
          <span>Posting</span>
          <select
            value={filters.posting}
            onChange={(event) =>
              onFiltersChange({
                ...filters,
                posting: event.target.value
              })
            }
            disabled={isBusy}
          >
            <option value="">All</option>
            <option value="posting">Posting</option>
            <option value="control">Control</option>
          </select>
        </label>
        <label className="form-field">
          <span>View</span>
          <select
            value={filters.viewMode}
            onChange={(event) =>
              onFiltersChange({
                ...filters,
                viewMode: event.target.value
              })
            }
            disabled={isBusy}
          >
            <option value="default">Default</option>
            <option value="all">All</option>
            <option value="headerTotal">Header+Total</option>
          </select>
        </label>
        <label className="form-field">
          <span>Level</span>
          <select
            value={filters.level}
            onChange={(event) =>
              onFiltersChange({
                ...filters,
                level: event.target.value
              })
            }
            disabled={isBusy}
          >
            <option value="">All levels</option>
            {legacyAccountLevels.map((level) => (
              <option key={level.code} value={level.code}>
                {level.code} {level.label}
              </option>
            ))}
          </select>
        </label>
        <label className="form-field">
          <span>Role</span>
          <select
            value={filters.role}
            onChange={(event) =>
              onFiltersChange({
                ...filters,
                role: event.target.value
              })
            }
            disabled={isBusy}
          >
            <option value="">All</option>
            {activeRanges.map((range) => (
              <option key={range.accountCodeRangeId} value={range.role}>
                {range.role}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="coa-level-legend client-panel" aria-label="Legacy account levels">
        {legacyAccountLevels.map((level) => (
          <button
            aria-pressed={filters.level === level.code}
            className={`coa-level-chip${filters.level === level.code ? " active" : ""}`}
            key={level.code}
            type="button"
            onClick={() =>
              onFiltersChange({
                ...filters,
                level: filters.level === level.code ? "" : level.code
              })
            }
            disabled={isBusy}
            title={`${level.code} ${level.label}`}
          >
            <strong>{level.code}</strong>
            <span>{level.label}</span>
          </button>
        ))}
      </div>

      <div className="coa-summary-row">
        <article className="client-panel coa-summary-card">
          <span>Shown</span>
          <strong>{visibleAccounts.length}</strong>
        </article>
        <article className="client-panel coa-summary-card">
          <span>Posting</span>
          <strong>{postingCount}</strong>
        </article>
        <article className="client-panel coa-summary-card">
          <span>Non-posting</span>
          <strong>{nonPostingCount}</strong>
        </article>
        <article className="client-panel coa-summary-card">
          <span>Ranges</span>
          <strong>{activeRanges.length}</strong>
        </article>
      </div>

      <div className="coa-layout">
        <section className="client-panel coa-range-panel">
          <div className="client-panel-heading">
            <div>
              <span>Accounting Setup</span>
              <strong>Ranges</strong>
            </div>
            <ListTree size={18} />
          </div>
          <div className="coa-range-list">
            {ranges.length === 0 ? (
              <div className="client-empty-state">No ranges loaded</div>
            ) : (
              ranges.map((range) => (
                <button
                  className={`coa-range-item${
                    selectedRangeRole === range.role || filters.role === range.role ? " active" : ""
                  }`}
                  key={range.accountCodeRangeId}
                  type="button"
                  onClick={() => handleSelectRange(range)}
                  disabled={isBusy}
                >
                  <span>
                    <strong>{range.role}</strong>
                    <small>{range.displayName}</small>
                  </span>
                  <em>{formatRange(range)}</em>
                </button>
              ))
            )}
          </div>

          <form className="coa-range-form" onSubmit={handleSaveRange}>
            <div className="billing-subform-heading">
              <SlidersHorizontal size={16} />
              <strong>Range rule</strong>
            </div>
            <div className="billing-form-grid coa-range-fields">
              <label className="form-field">
                <span>Role</span>
                <input value={selectedRange?.role ?? ""} disabled={isBusy} readOnly />
              </label>
              <label className="form-field">
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
            <div className="billing-action-row">
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

        <div className="coa-register-stack">
          <section className="client-panel coa-account-panel">
            <div className="client-panel-heading">
              <div>
                <span>{accountMode === "edit" ? "Account Maintenance" : "Account Setup"}</span>
                <strong>{accountMode === "edit" ? "Edit ledger account" : "New ledger account"}</strong>
              </div>
              <div className="coa-inline-actions">
                <button
                  className="icon-button"
                  type="button"
                  onClick={() => void onNewAccount()}
                  disabled={isBusy}
                  title="Start a new ledger account"
                >
                  <Plus size={16} />
                  New
                </button>
                <button
                  className="icon-button"
                  type="button"
                  onClick={onSuggestAccountCode}
                  disabled={isBusy || !canSuggestAccountCode}
                  title="Suggest the next code for the selected range"
                >
                  <Wand2 size={16} />
                  Suggest
                </button>
              </div>
            </div>
            <form className="coa-account-form" onSubmit={handleSaveAccount}>
              <div className="billing-form-grid coa-account-fields">
                <label className="form-field">
                  <span>Code</span>
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
                  <span>Name</span>
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
                  <span>Type</span>
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
                  <span>Level</span>
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
                  <span>{accountMode === "edit" ? "Parent account" : "Parent code"}</span>
                  <input
                    value={
                      accountMode === "edit"
                        ? accountValue.parentAccountId
                        : selectedRange?.parentCode ?? ""
                    }
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
                    disabled={isBusy || accountMode === "create"}
                  >
                    <option value="Active">Active</option>
                    <option value="Inactive">Inactive</option>
                  </select>
                </label>
              </div>
              <div className="coa-range-flags">
                <label>
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
              <div className="billing-action-row">
                <button
                  className="icon-button primary"
                  type="submit"
                  disabled={isBusy || !canSaveAccount}
                  title={accountMode === "edit" ? "Save ledger account" : "Create ledger account"}
                >
                  <Save size={16} />
                  {accountMode === "edit" ? "Save account" : "Create account"}
                </button>
                {selectedRange !== null && accountMode === "create" && (
                  <span className="billing-small-fact">
                    {selectedRange.displayName}
                  </span>
                )}
              </div>
            </form>
          </section>

          <section className="client-panel coa-table-panel">
            <div className="client-panel-heading">
              <div>
                <span>Register</span>
                <strong>Ledger accounts</strong>
              </div>
              <span className="billing-small-fact">{visibleAccounts.length} of {accounts.length}</span>
            </div>
            <table className="coa-table">
              <thead>
                <tr>
                  <th>Code</th>
                  <th>Name</th>
                  <th>Level</th>
                  <th>Role</th>
                  <th>Type</th>
                  <th>Balance</th>
                  <th>Posting</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {visibleAccounts.length === 0 ? (
                  <tr>
                    <td colSpan={9}>No ledger accounts for the current filters</td>
                  </tr>
                ) : (
                  visibleAccounts.map((account) => {
                    const level = getLegacyAccountLevel(account, ranges);

                    return (
                      <tr key={account.ledgerAccountId}>
                        <td>
                          <strong>{account.displayCode}</strong>
                        </td>
                        <td>{account.name}</td>
                        <td>
                          <span className={`coa-level-badge ${level.code.toLowerCase()}`}>
                            {level.code}
                          </span>
                          <small className="coa-level-label">{level.label}</small>
                        </td>
                        <td>{account.rangeRole ?? "-"}</td>
                        <td>{account.type}</td>
                        <td>{account.normalBalance}</td>
                        <td>{account.isPostingAccount ? "Posting" : "Non-posting"}</td>
                        <td>
                          <span className={`status-pill ${account.status.toLowerCase()}`}>
                            {account.status}
                          </span>
                        </td>
                        <td>
                          <div className="coa-row-actions">
                            <button
                              className="table-icon-button"
                              type="button"
                              onClick={() => onEditAccount(account)}
                              disabled={isBusy}
                              title="Edit ledger account"
                            >
                              <Pencil size={14} />
                            </button>
                            <button
                              className="table-icon-button"
                              type="button"
                              onClick={() => void onViewAccountActivity(account)}
                              disabled={isBusy}
                              title="View account activity"
                            >
                              <BookOpen size={14} />
                            </button>
                            <button
                              className="table-icon-button"
                              type="button"
                              onClick={() => void onToggleAccountStatus(account)}
                              disabled={isBusy}
                              title={
                                account.status === "Active"
                                  ? "Deactivate ledger account"
                                  : "Reactivate ledger account"
                              }
                            >
                              {account.status === "Active" ? (
                                <Power size={14} />
                              ) : (
                                <RotateCcw size={14} />
                              )}
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </section>

          {activity !== null && (
            <section className="client-panel coa-activity-panel">
              <div className="client-panel-heading">
                <div>
                  <span>{activity.code}</span>
                  <strong>{activity.name}</strong>
                </div>
                <span className="billing-small-fact">
                  {formatActivityWindow(activity)} - {activity.currencyCode ?? "No currency"}
                </span>
              </div>
              <div className="coa-activity-summary">
                <ActivityFact label="Opening" value={formatMoney(activity.openingBalance)} />
                <ActivityFact label="Debit" value={formatMoney(activity.periodDebit)} />
                <ActivityFact label="Credit" value={formatMoney(activity.periodCredit)} />
                <ActivityFact label="Ending" value={formatMoney(activity.endingBalance)} />
              </div>
              <table className="coa-activity-table">
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Source</th>
                    <th>Reference</th>
                    <th>Status</th>
                    <th>Debit</th>
                    <th>Credit</th>
                    <th>Balance</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {activity.lines.length === 0 ? (
                    <tr>
                      <td colSpan={8}>No account activity</td>
                    </tr>
                  ) : (
                    activity.lines.map((line) => (
                      <tr key={`${line.journalEntryId}-${line.entryDate}-${line.runningBalance}`}>
                        <td>{line.entryDate}</td>
                        <td>{line.sourceType}</td>
                        <td>{line.sourceReference ?? "-"}</td>
                        <td>
                          <span className={`status-pill ${line.status.toLowerCase()}`}>
                            {line.status}
                          </span>
                        </td>
                        <td>{formatMoney(line.debit)}</td>
                        <td>{formatMoney(line.credit)}</td>
                        <td>{formatMoney(line.runningBalance)}</td>
                        <td>
                          <button
                            className={`table-icon-button${
                              hasJournalEntry(line.journalEntryId, journalEntries) ? "" : " muted"
                            }`}
                            type="button"
                            onClick={() => void onViewJournalEntry(line)}
                            title="View journal lines"
                          >
                            <BookOpen size={14} />
                          </button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </section>
          )}
        </div>
      </div>
    </section>
  );
}

function formatRange(range: AccountCodeRange): string {
  return `${range.rangeStart}-${range.rangeEnd}`;
}

function ActivityFact({
  label,
  value
}: {
  label: string;
  value: string;
}) {
  return (
    <span>
      <small>{label}</small>
      <strong>{value}</strong>
    </span>
  );
}

function formatActivityWindow(activity: LedgerAccountActivity): string {
  if ((activity.fromDate ?? "").trim() === "" && (activity.toDate ?? "").trim() === "") {
    return "All dates";
  }

  if ((activity.fromDate ?? "").trim() === "") {
    return `Through ${activity.toDate}`;
  }

  if ((activity.toDate ?? "").trim() === "") {
    return `From ${activity.fromDate}`;
  }

  return `${activity.fromDate} to ${activity.toDate}`;
}

function hasJournalEntry(
  journalEntryId: string,
  journalEntries: JournalEntrySummary[]
): boolean {
  return journalEntries.some((entry) => entry.journalEntryId === journalEntryId);
}
