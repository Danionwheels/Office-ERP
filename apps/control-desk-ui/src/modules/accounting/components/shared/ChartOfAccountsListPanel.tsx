import {
  BookOpen,
  Check,
  ListTree,
  Pencil,
  Power,
  RotateCcw,
  X
} from "lucide-react";
import { Fragment } from "react";
import type { ApiErrorItem } from "../../../../shared/api/apiError";
import {
  accountingCurrencyCode,
  legacyAccountCreateLevels,
  legacyAccountLevels,
  type LegacyAccountLevel,
  type LegacyAccountLevelCode
} from "../../constants/accountingConstants";
import type {
  AccountCodeRange,
  LedgerAccountCreateContext,
  LedgerAccountEditorInput,
  LedgerAccountFilters,
  LedgerAccountReconciliation,
  LedgerAccountRepairPlan,
  LedgerAccountSummary
} from "../../types/accountingTypes";
import {
  compareChildCreateRanges,
  formatAccountSaveError,
  formatAccountTreeHeading,
  getCoaTreeRowContextItems,
  getDefaultChildLevelCodeForRange,
  getHighestIssueSeverity,
  getLegacyLevelLabel,
  isUsableChildRangeForAccount,
  type AccountTreeRow,
  type CoaRangeFact
} from "../../utils/chartOfAccountsWorkspaceModel";

type ChildCreateOption = {
  context: LedgerAccountCreateContext | null;
  disabledReason: string | null;
  key: string;
  label: string;
  levelCode: LegacyAccountLevelCode;
  range: AccountCodeRange | null;
};

type ChartOfAccountsListPanelProps = {
  accountMode: "create" | "edit";
  accountFormId: string;
  accountSaveErrors: ApiErrorItem[];
  accountTreeRows: AccountTreeRow[];
  accountValue: LedgerAccountEditorInput;
  accounts: LedgerAccountSummary[];
  activeRanges: AccountCodeRange[];
  canSaveAccount: boolean;
  collapsedAccountIds: Set<string>;
  collapsedVisibleParentCount: number;
  collapsibleAccountIds: string[];
  filters: LedgerAccountFilters;
  inlineCreateAnchorId: string | null;
  isBusy: boolean;
  isParentTreeView: boolean;
  openVisibleParentCount: number;
  postingCount: number;
  reconciliation: LedgerAccountReconciliation | null;
  repairPlan: LedgerAccountRepairPlan | null;
  selectedRangeFacts: CoaRangeFact[];
  selectedRangeRole: string;
  showRangeSetup: boolean;
  visibleAccounts: LedgerAccountSummary[];
  onAccountChange: (value: LedgerAccountEditorInput) => void;
  onCancelInlineCreate: () => void;
  onCollapseAllTree: () => void;
  onEditAccount: (account: LedgerAccountSummary) => void;
  onExpandAllTree: () => void;
  onFiltersChange: (value: LedgerAccountFilters) => void;
  onRangeRoleSelect: (role: string) => void;
  onSaveAccount: () => Promise<void>;
  onStartInlineCreate: (
    account: LedgerAccountSummary,
    context: LedgerAccountCreateContext,
    label: string
  ) => Promise<void>;
  onToggleAccountCollapse: (accountId: string) => void;
  onToggleAccountStatus: (account: LedgerAccountSummary) => Promise<void>;
  onToggleRangeSetup: () => void;
  onViewAccountActivity: (account: LedgerAccountSummary) => Promise<void>;
};

export function ChartOfAccountsListPanel({
  accountMode,
  accountFormId,
  accountSaveErrors,
  accountTreeRows,
  accountValue,
  accounts,
  activeRanges,
  canSaveAccount,
  collapsedAccountIds,
  collapsedVisibleParentCount,
  collapsibleAccountIds,
  filters,
  inlineCreateAnchorId,
  isBusy,
  isParentTreeView,
  openVisibleParentCount,
  postingCount,
  reconciliation,
  repairPlan,
  selectedRangeFacts,
  selectedRangeRole,
  showRangeSetup,
  visibleAccounts,
  onAccountChange,
  onCancelInlineCreate,
  onCollapseAllTree,
  onEditAccount,
  onExpandAllTree,
  onFiltersChange,
  onRangeRoleSelect,
  onSaveAccount,
  onStartInlineCreate,
  onToggleAccountCollapse,
  onToggleAccountStatus,
  onToggleRangeSetup,
  onViewAccountActivity
}: ChartOfAccountsListPanelProps) {
  const accountsById = new Map(accounts.map((account) => [account.ledgerAccountId, account]));
  const reconciliationByAccountId = new Map(
    reconciliation?.items.map((item) => [item.ledgerAccountId, item]) ?? []
  );
  const repairActionsByAccountId = new Map(
    repairPlan?.items.map((item) => [item.ledgerAccountId, item.actions]) ?? []
  );
  const inlineAnchorIndex =
    accountMode === "create" && inlineCreateAnchorId !== null
      ? accountTreeRows.findIndex((row) => row.account.ledgerAccountId === inlineCreateAnchorId)
      : -1;
  const inlineAnchorRow = inlineAnchorIndex >= 0 ? accountTreeRows[inlineAnchorIndex] : null;
  const createLevelRankByCode = new Map(
    legacyAccountCreateLevels.map((level, index) => [level.code, index])
  );

  function getChildCreateOptions(
    account: LedgerAccountSummary,
    level: LegacyAccountLevel
  ): ChildCreateOption[] {
    const availableOptions = activeRanges
      .filter((range) => isUsableChildRangeForAccount(range, account, level, accounts))
      .map((range) => {
        const levelCode = getDefaultChildLevelCodeForRange(range, level);
        const levelLabel = getLegacyLevelLabel(levelCode);

        return {
          context: {
            rangeRole: range.role,
            parentAccountId: account.ledgerAccountId,
            level: levelLabel,
            isPostingAccount: range.isPostingAccount
          },
          disabledReason: null,
          key: `${range.role}:${levelCode}:${range.isPostingAccount ? "posting" : "control"}`,
          label: levelLabel,
          levelCode,
          range
        };
      })
      .filter((option) => option.levelCode !== "D")
      .sort((left, right) => compareChildCreateRanges(left.range, right.range, account, level));

    return legacyAccountCreateLevels.map((createLevel) => {
      const matchingOptions = availableOptions
        .filter((option) => option.levelCode === createLevel.code);
      const bestOption = matchingOptions[0] ?? null;

      if (bestOption === null) {
        return {
          context: null,
          disabledReason: `No active ${createLevel.label} range is available for ${account.displayCode}.`,
          key: `unavailable:${createLevel.code}`,
          label: `${createLevel.label} (no range)`,
          levelCode: createLevel.code,
          range: null
        };
      }

      return {
        ...bestOption,
        label: matchingOptions.length > 1
          ? `${createLevel.label} / ${bestOption.range.displayName}`
          : createLevel.label
      };
    });
  }

  function getCurrentInlineCreateOptionKey(options: ChildCreateOption[]): string {
    return options.find((option) =>
      option.context !== null
      && option.context.rangeRole === selectedRangeRole
      && option.context.level === accountValue.level
      && option.context.isPostingAccount === accountValue.isPostingAccount)?.key
      ?? options.find((option) =>
        option.context !== null && option.context.rangeRole === selectedRangeRole)?.key
      ?? options.find((option) => option.context !== null)?.key
      ?? options[0]?.key
      ?? "";
  }

  function shouldRenderInlineCreateAfterRow(
    rowIndex: number,
    anchorIndex: number,
    anchorDepth: number
  ): boolean {
    if (anchorIndex < 0 || rowIndex < anchorIndex) {
      return false;
    }

    const row = accountTreeRows[rowIndex];
    const nextRow = accountTreeRows[rowIndex + 1];

    if (rowIndex === anchorIndex) {
      return nextRow === undefined || nextRow.depth <= anchorDepth;
    }

    return row.depth > anchorDepth && (nextRow === undefined || nextRow.depth <= anchorDepth);
  }

  return (
    <section className="entry-section coa-account-list-panel">
      <div className="section-heading-row">
        <h2>Account List</h2>
        <span>
          {formatAccountTreeHeading(
            accountTreeRows.length,
            visibleAccounts.length,
            accounts.length,
            postingCount,
            isParentTreeView
          )}
        </span>
      </div>

      <div className="coa-list-filter-row">
        <label className="form-field">
          <span>Range</span>
          <select
            value={filters.role}
            onChange={(event) => onRangeRoleSelect(event.target.value)}
            disabled={isBusy}
          >
            <option value="">All ranges</option>
            {activeRanges.map((range) => (
              <option key={range.role} value={range.role}>
                {range.displayName}
              </option>
            ))}
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
            <option value="headerTotal">Total parent tree</option>
          </select>
        </label>
      </div>

      <div className="coa-tree-action-strip">
        <span className={isParentTreeView ? "ready" : "neutral"}>
          <small>View</small>
          <strong>{isParentTreeView ? "Total parent tree" : "Nested list"}</strong>
        </span>
        <span>
          <small>Total rows</small>
          <strong>{collapsibleAccountIds.length}</strong>
        </span>
        <span className={openVisibleParentCount > 0 ? "ready" : "neutral"}>
          <small>Open</small>
          <strong>{openVisibleParentCount}</strong>
        </span>
        <span className={collapsedVisibleParentCount > 0 ? "warning" : "neutral"}>
          <small>Closed</small>
          <strong>{collapsedVisibleParentCount}</strong>
        </span>
        <div className="coa-tree-action-buttons">
          <button
            className="icon-button"
            type="button"
            onClick={onExpandAllTree}
            disabled={isBusy || collapsedVisibleParentCount === 0}
            title="Expand all visible parent accounts"
          >
            <ListTree size={15} />
            Expand all
          </button>
          <button
            className="icon-button"
            type="button"
            onClick={onCollapseAllTree}
            disabled={isBusy || collapsibleAccountIds.length === 0}
            title="Collapse all visible parent accounts"
          >
            <ListTree size={15} />
            Collapse all
          </button>
          <button
            className={`icon-button${showRangeSetup ? " primary" : ""}`}
            type="button"
            onClick={onToggleRangeSetup}
            disabled={isBusy}
            title={showRangeSetup ? "Hide range setup" : "Show range setup"}
          >
            <ListTree size={15} />
            Ranges
          </button>
        </div>
      </div>

      <div className="coa-selected-range-strip" aria-label="Selected range rule">
        {selectedRangeFacts.length === 0 ? (
          <span className="warning">
            <small>Range</small>
            <strong>All ranges</strong>
          </span>
        ) : (
          selectedRangeFacts.map((fact) => (
            <span className={fact.tone} key={fact.label} title={fact.title}>
              <small>{fact.label}</small>
              <strong>{fact.value}</strong>
            </span>
          ))
        )}
      </div>

      <div className="coa-account-list-frame">
        <table className="coa-table coa-account-list-table coa-tree-table">
          <thead>
            <tr>
              <th aria-label="Selected account" />
              <th aria-label="Expand or add child account" />
              <th>Acc Type</th>
              <th>Acc Code</th>
              <th>Account Name</th>
              <th>CUR</th>
              <th>Loc</th>
              <th>NNI</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {accountTreeRows.length === 0 ? (
              <tr>
                <td colSpan={9}>No ledger accounts for the current filters</td>
              </tr>
            ) : (
              accountTreeRows.map((row, rowIndex) => {
                const { account, childCount, depth, hasChildren, isMatched, level, parentAccountId } = row;
                const isSelected = accountMode === "edit" && accountValue.code === account.code;
                const isCollapsed = collapsedAccountIds.has(account.ledgerAccountId);
                const depthClass = `depth-${Math.min(depth, 5)}`;
                const childCreateOptions = getChildCreateOptions(account, level);
                const childCreateContext =
                  childCreateOptions.find((option) => option.context !== null)?.context ?? null;
                const reconciliationItem = reconciliationByAccountId.get(account.ledgerAccountId);
                const rowIssues = reconciliationItem?.issues ?? [];
                const repairActions = repairActionsByAccountId.get(account.ledgerAccountId) ?? [];
                const rowIssueSeverity = getHighestIssueSeverity(rowIssues);
                const parentAccount = parentAccountId === null
                  ? null
                  : accountsById.get(parentAccountId) ?? null;
                const rowContextItems = getCoaTreeRowContextItems(account, level, {
                  childCreateContext,
                  depth,
                  parentAccount
                });
                const shouldRenderInlineCreate =
                  inlineAnchorRow !== null
                  && inlineAnchorRow.account.ledgerAccountId === inlineCreateAnchorId
                  && shouldRenderInlineCreateAfterRow(rowIndex, inlineAnchorIndex, inlineAnchorRow.depth);
                const inlineCreateOptions = inlineAnchorRow === null
                  ? []
                  : [...getChildCreateOptions(inlineAnchorRow.account, inlineAnchorRow.level)]
                    .sort((left, right) => {
                      const leftRank = createLevelRankByCode.get(left.levelCode) ?? 99;
                      const rightRank = createLevelRankByCode.get(right.levelCode) ?? 99;

                      return leftRank - rightRank || left.label.localeCompare(right.label);
                    });
                const inlineCreateDepthClass = inlineAnchorRow === null
                  ? "depth-1"
                  : `depth-${Math.min(inlineAnchorRow.depth + 1, 5)}`;
                const currentInlineCreateOptionKey = getCurrentInlineCreateOptionKey(inlineCreateOptions);
                const inlineCreateHasAvailableOption =
                  inlineCreateOptions.some((option) => option.context !== null);

                return (
                  <Fragment key={account.ledgerAccountId}>
                    <tr
                      className={[
                        "coa-tree-row",
                        depthClass,
                        `level-${level.code.toLowerCase()}`,
                        hasChildren ? "parent-row" : "leaf-row",
                        hasChildren && isCollapsed ? "collapsed" : "",
                        hasChildren && !isCollapsed ? "expanded" : "",
                        rowIssues.length > 0 ? `has-issues issue-${rowIssueSeverity}` : "",
                        isSelected ? "active" : "",
                        accountMode === "create" && inlineCreateAnchorId === account.ledgerAccountId
                          ? "creating-child"
                          : "",
                        isMatched ? "" : "context"
                      ].filter(Boolean).join(" ")}
                      onDoubleClick={() => onEditAccount(account)}
                    >
                      <td className="coa-tree-selector-cell">
                        <span aria-label={isSelected ? "Selected account" : "Not selected"} />
                      </td>
                      <td className="coa-tree-expander-cell">
                        <span className={`coa-tree-expander-group${hasChildren ? " has-children" : ""}`}>
                          {hasChildren ? (
                            <button
                              className="coa-tree-expander"
                              type="button"
                              onClick={() => {
                                if (!isCollapsed && inlineCreateAnchorId === account.ledgerAccountId) {
                                  onCancelInlineCreate();
                                }

                                onToggleAccountCollapse(account.ledgerAccountId);

                                if (isCollapsed && childCreateContext !== null) {
                                  void onStartInlineCreate(
                                    account,
                                    childCreateContext,
                                    `Child account / ${account.displayCode}`
                                  );
                                }
                              }}
                              disabled={isBusy}
                              aria-label={isCollapsed ? "Expand child accounts" : "Collapse child accounts"}
                              title={isCollapsed && childCreateContext !== null
                                ? `Expand and create child under ${account.displayCode}`
                                : isCollapsed
                                  ? "Expand account"
                                  : "Collapse account"}
                            >
                              {isCollapsed ? "+" : "-"}
                            </button>
                          ) : childCreateContext !== null ? (
                            <button
                              className="coa-tree-expander create"
                              type="button"
                              onClick={() => {
                                void onStartInlineCreate(
                                  account,
                                  childCreateContext,
                                  `Child account / ${account.displayCode}`
                                );
                              }}
                              disabled={isBusy}
                              aria-label={`Add child account under ${account.displayCode}`}
                              title={`Create child under ${account.displayCode}`}
                            >
                              +
                            </button>
                          ) : (
                            <span className="coa-tree-expander placeholder" aria-hidden="true" />
                          )}
                        </span>
                      </td>
                      <td>
                        <span
                          className={`coa-account-type-chip ${level.code.toLowerCase()}`}
                          title={level.label}
                        >
                          {level.label}
                        </span>
                      </td>
                      <td>
                        <strong>{account.displayCode}</strong>
                      </td>
                      <td title={account.name}>
                        <span className="coa-account-list-name">
                          <strong>{account.name}</strong>
                          <span className="coa-tree-row-context">
                            {rowContextItems.map((item) => (
                              <small className={item.tone} key={`${item.title}-${item.label}`} title={item.title}>
                                {item.label}
                              </small>
                            ))}
                          </span>
                          {rowIssues.length > 0 && (
                            <small
                              className={`coa-account-issue-badge ${rowIssueSeverity}`}
                              title={rowIssues.map((issue) => issue.message).join(" / ")}
                            >
                              {rowIssues.length} issue{rowIssues.length === 1 ? "" : "s"}
                              {repairActions.length > 0
                                ? ` / ${repairActions.length} repair`
                                : ""}
                            </small>
                          )}
                          {account.status !== "Active" && <small>{account.status}</small>}
                          {hasChildren && (
                            <small className="coa-child-count-badge">
                              {childCount} child{childCount === 1 ? "" : "ren"}
                              {isCollapsed ? " hidden" : " shown"}
                            </small>
                          )}
                        </span>
                      </td>
                      <td>{accountingCurrencyCode}</td>
                      <td className="coa-grid-check-cell">
                        <span className="coa-grid-check" aria-hidden="true" />
                      </td>
                      <td className="coa-grid-check-cell">
                        <span className="coa-grid-check" aria-hidden="true" />
                      </td>
                      <td>
                        <div className="coa-row-actions">
                          <button
                            className="table-icon-button"
                            type="button"
                            onClick={() => onEditAccount(account)}
                            disabled={isBusy}
                            aria-label={`Edit ${account.displayCode}`}
                            title="Edit ledger account"
                          >
                            <Pencil size={14} />
                          </button>
                          <button
                            className="table-icon-button"
                            type="button"
                            onClick={() => void onViewAccountActivity(account)}
                            disabled={isBusy}
                            aria-label={`View activity for ${account.displayCode}`}
                            title="View account activity"
                          >
                            <BookOpen size={14} />
                          </button>
                          <button
                            className="table-icon-button"
                            type="button"
                            onClick={() => void onToggleAccountStatus(account)}
                            disabled={isBusy}
                            aria-label={
                              account.status === "Active"
                                ? `Deactivate ${account.displayCode}`
                                : `Reactivate ${account.displayCode}`
                            }
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
                    {shouldRenderInlineCreate && inlineAnchorRow !== null && (
                      <>
                        <tr className={`coa-inline-grid-create-row ${inlineCreateDepthClass}`}>
                          <td className="coa-tree-new-row-star" title="New child account">
                            *
                          </td>
                          <td className="coa-tree-expander-cell">
                            <button
                              className="coa-tree-expander"
                              type="button"
                              onClick={onCancelInlineCreate}
                              disabled={isBusy}
                              aria-label="Close child account row"
                              title="Close child account row"
                            >
                              -
                            </button>
                          </td>
                          <td>
                            <select
                              form={accountFormId}
                              value={currentInlineCreateOptionKey}
                              onChange={(event) => {
                                const selectedOption = inlineCreateOptions.find((option) =>
                                  option.key === event.target.value);

                                if (selectedOption === undefined || selectedOption.context === null) {
                                  return;
                                }

                                void onStartInlineCreate(
                                  inlineAnchorRow.account,
                                  selectedOption.context,
                                  `Child account / ${inlineAnchorRow.account.displayCode}`
                                );
                              }}
                              disabled={isBusy || !inlineCreateHasAvailableOption}
                              aria-label={`Account type for child under ${inlineAnchorRow.account.displayCode}`}
                              title={`Account type for child under ${inlineAnchorRow.account.displayCode}`}
                            >
                              {inlineCreateOptions.map((option) => (
                                <option
                                  key={option.key}
                                  value={option.key}
                                  disabled={option.context === null}
                                >
                                  {option.label}
                                </option>
                              ))}
                            </select>
                          </td>
                          <td>
                            <input
                              form={accountFormId}
                              value={accountValue.code}
                              readOnly
                              placeholder="Auto"
                              aria-label="Generated account code"
                              title="Generated account code"
                            />
                          </td>
                          <td>
                            <input
                              form={accountFormId}
                              value={accountValue.name}
                              onChange={(event) =>
                                onAccountChange({
                                  ...accountValue,
                                  name: event.target.value
                                })
                              }
                              onKeyDown={(event) => {
                                if (event.key === "Escape") {
                                  onCancelInlineCreate();
                                }
                              }}
                              placeholder="Account name"
                              disabled={isBusy}
                              autoFocus
                              aria-label={`Account name for child under ${inlineAnchorRow.account.displayCode}`}
                              title={`Parent ${inlineAnchorRow.account.displayCode} linked automatically`}
                            />
                          </td>
                          <td>{accountingCurrencyCode}</td>
                          <td className="coa-grid-check-cell">
                            <span className="coa-grid-check" aria-hidden="true" />
                          </td>
                          <td className="coa-grid-check-cell">
                            <span className="coa-grid-check" aria-hidden="true" />
                          </td>
                          <td>
                            <div className="coa-row-actions">
                              <button
                                className="table-icon-button"
                                type="button"
                                onClick={() => void onSaveAccount()}
                                disabled={isBusy || !canSaveAccount}
                                aria-label="Save child account"
                                title="Save child account"
                              >
                                <Check size={14} />
                              </button>
                              <button
                                className="table-icon-button"
                                type="button"
                                onClick={onCancelInlineCreate}
                                disabled={isBusy}
                                aria-label="Cancel child account"
                                title="Cancel child account"
                              >
                                <X size={14} />
                              </button>
                            </div>
                          </td>
                        </tr>
                        {accountSaveErrors.length > 0 && (
                          <tr className="coa-inline-grid-create-errors">
                            <td colSpan={9}>
                              {accountSaveErrors.map((error) => (
                                <span key={`${error.target ?? "account"}:${error.message}`}>
                                  {formatAccountSaveError(error)}
                                </span>
                              ))}
                            </td>
                          </tr>
                        )}
                      </>
                    )}
                  </Fragment>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
