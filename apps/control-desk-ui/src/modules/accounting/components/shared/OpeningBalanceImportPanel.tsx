import {
  ClipboardList,
  FileCheck2,
  Plus,
  Send,
  Trash2
} from "lucide-react";
import { type FormEvent, useState } from "react";
import type {
  LedgerAccountSummary,
  OpeningBalanceImportInput,
  OpeningBalanceImportLineInput,
  OpeningBalanceImportPreview,
  OpeningBalanceImportTemplateFormat,
  OpeningBalanceImportTextPreview
} from "../../types/accountingTypes";
import {
  amount,
  formatMoney,
  roundMoney
} from "../../utils/journalModel";
import {
  createEmptyOpeningBalanceLine,
  findLedgerAccount,
  findOpeningBalanceAccount,
  formatJournalAccountContext,
  formatJournalAccountOption,
  formatOpeningBalanceAccountContext,
  formatOpeningBalanceAccountOption,
  formatOpeningBalanceMatchedAccountLabel,
  formatOpeningBalancePreviewAccountMeta,
  getActivePostingAccounts,
  getOpeningBalanceAccountScope,
  getOpeningBalanceLineState,
  getOpeningBalancePreviewLineSide,
  getOpeningBalanceProfitLossCarryForwardAccounts,
  getOpeningBalanceProfilePostBlocker,
  getOpeningBalanceProfileReadiness,
  getOpeningBalanceReadiness,
  getOpeningBalanceTemplateTitle,
  openingBalanceAccountListId,
  openingBalanceLineStateLabel,
  openingBalanceTemplateOptions,
  withSelectedJournalAccount
} from "../../utils/journalWorkbenchModel";

type OpeningBalanceImportPanelProps = {
  accounts: LedgerAccountSummary[];
  value: OpeningBalanceImportInput;
  preview: OpeningBalanceImportPreview | null;
  importText: string;
  importDelimiter: string;
  textPreview: OpeningBalanceImportTextPreview | null;
  isBusy: boolean;
  onValueChange: (value: OpeningBalanceImportInput) => void;
  onPreviewOpeningBalance: () => Promise<void>;
  onImportTextChange: (value: string) => void;
  onImportDelimiterChange: (value: string) => void;
  onPreviewImportText: () => Promise<void>;
  onUseTemplate: (format?: OpeningBalanceImportTemplateFormat) => void;
  onSaveProfile: () => Promise<void>;
  onPostOpeningBalance: () => Promise<void>;
};

export function OpeningBalanceImportPanel({
  accounts,
  value,
  preview,
  importText,
  importDelimiter,
  textPreview,
  isBusy,
  onValueChange,
  onPreviewOpeningBalance,
  onImportTextChange,
  onImportDelimiterChange,
  onPreviewImportText,
  onUseTemplate,
  onSaveProfile,
  onPostOpeningBalance
}: OpeningBalanceImportPanelProps) {
  const [openingBalanceTemplateFormat, setOpeningBalanceTemplateFormat] =
    useState<OpeningBalanceImportTemplateFormat>("legacy-sql");
  const postingAccounts = getActivePostingAccounts(accounts);
  const openingTotalDebit = value.lines.reduce(
    (total, line) => total + amount(line.debit),
    0
  );
  const openingTotalCredit = value.lines.reduce(
    (total, line) => total + amount(line.credit),
    0
  );
  const openingDifference = roundMoney(openingTotalDebit - openingTotalCredit);
  const openingBalanceReadinessItems = getOpeningBalanceReadiness({
    accounts: postingAccounts,
    currencyCode: value.currencyCode,
    difference: openingDifference,
    entryDate: value.entryDate,
    lines: value.lines,
    preview,
    totalCredit: openingTotalCredit,
    totalDebit: openingTotalDebit
  });
  const openingBalanceScopeItems = getOpeningBalanceAccountScope(postingAccounts);
  const selectedOpeningBalanceCarryForwardAccount =
    findLedgerAccount(value.profitAndLossCarryForwardAccountId, postingAccounts);
  const openingBalanceCarryForwardAccountOptions = withSelectedJournalAccount(
    getOpeningBalanceProfitLossCarryForwardAccounts(postingAccounts),
    selectedOpeningBalanceCarryForwardAccount
  );
  const openingBalanceProfileReadiness = getOpeningBalanceProfileReadiness(
    value,
    selectedOpeningBalanceCarryForwardAccount
  );
  const openingBalanceProfilePostBlocker = getOpeningBalanceProfilePostBlocker(value);
  const openingBalanceCanPost =
    preview?.canPost === true
    && openingBalanceProfilePostBlocker === null;
  const openingBalancePostTitle = openingBalanceProfilePostBlocker?.title
    ?? (preview?.canPost === true
      ? "Post opening balance journal"
      : "Preview and balance opening balances before posting");

  async function handlePreviewOpeningBalance(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onPreviewOpeningBalance();
  }

  function updateOpeningBalanceLine(
    index: number,
    patch: Partial<OpeningBalanceImportLineInput>
  ) {
    onValueChange({
      ...value,
      lines: value.lines.map((line, lineIndex) =>
        lineIndex === index ? { ...line, ...patch } : line)
    });
  }

  function addOpeningBalanceLine() {
    onValueChange({
      ...value,
      lines: [
        ...value.lines,
        createEmptyOpeningBalanceLine()
      ]
    });
  }

  function removeOpeningBalanceLine(index: number) {
    onValueChange({
      ...value,
      lines: value.lines.filter((_, lineIndex) => lineIndex !== index)
    });
  }

  return (
    <section className="client-panel opening-balance-panel">
      <div className="client-panel-heading journal-window-heading">
        <div>
          <span>Setup Utility</span>
          <strong>Opening Balance Import</strong>
        </div>
        <span className={`status-pill ${preview?.canPost ? "open" : "draft"}`}>
          {preview === null ? "Ready" : preview.canPost ? "Balanced" : "Blocked"}
        </span>
      </div>
      <form className="journal-form" onSubmit={handlePreviewOpeningBalance}>
        <div className="billing-form-grid journal-opening-fields">
          <label className="form-field">
            <span>Date</span>
            <input
              type="date"
              value={value.entryDate}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  entryDate: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Currency</span>
            <input
              value={value.currencyCode}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  currencyCode: event.target.value.toUpperCase()
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Reference</span>
            <input
              value={value.sourceReference}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  sourceReference: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field wide">
            <span>Memo</span>
            <input
              value={value.memo}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  memo: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
        </div>
        <div className="opening-balance-profile-strip">
          <label className="form-field">
            <span>FY From</span>
            <input
              type="date"
              value={value.profileFromDate}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  profileFromDate: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>FY To</span>
            <input
              type="date"
              value={value.profileToDate}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  profileToDate: event.target.value
                })
              }
              disabled={isBusy}
            />
          </label>
          <label className="form-field">
            <span>Status</span>
            <select
              value={value.profileStatus}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  profileStatus: event.target.value === "closed" ? "closed" : "open"
                })
              }
              disabled={isBusy}
            >
              <option value="open">Open</option>
              <option value="closed">Closed</option>
            </select>
          </label>
          <label className={`opening-balance-switch-field ${value.transactionsAllowed ? "ready" : "warning"}`}>
            <input
              type="checkbox"
              checked={value.transactionsAllowed}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  transactionsAllowed: event.target.checked
                })
              }
              disabled={isBusy}
            />
            <span>Txn allowed</span>
            <strong>{value.transactionsAllowed ? "Yes" : "No"}</strong>
          </label>
          <label className="form-field wide">
            <span>PL Carry-forward A/C</span>
            <select
              value={value.profitAndLossCarryForwardAccountId}
              onChange={(event) =>
                onValueChange({
                  ...value,
                  profitAndLossCarryForwardAccountId: event.target.value
                })
              }
              disabled={isBusy || openingBalanceCarryForwardAccountOptions.length === 0}
            >
              <option value="">Select account</option>
              {openingBalanceCarryForwardAccountOptions.map((account) => (
                <option key={account.ledgerAccountId} value={account.ledgerAccountId}>
                  {formatJournalAccountOption(account)}
                </option>
              ))}
            </select>
            {selectedOpeningBalanceCarryForwardAccount !== null && (
              <span className="journal-account-context">
                <strong>{selectedOpeningBalanceCarryForwardAccount.displayCode}</strong>
                <small>{formatJournalAccountContext(selectedOpeningBalanceCarryForwardAccount)}</small>
              </span>
            )}
          </label>
          <div className="opening-balance-profile-actions">
            <button
              className="secondary"
              type="button"
              onClick={() => void onSaveProfile()}
              disabled={isBusy || selectedOpeningBalanceCarryForwardAccount === null}
              title={
                selectedOpeningBalanceCarryForwardAccount === null
                  ? "Select the profit and loss carry-forward account first."
                  : "Save opening balance fiscal-year profile"
              }
            >
              <FileCheck2 size={16} />
              Save profile
            </button>
          </div>
        </div>
        <datalist id={openingBalanceAccountListId}>
          {postingAccounts.map((account) => (
            <option key={account.ledgerAccountId} value={account.code}>
              {formatOpeningBalanceAccountOption(account)}
            </option>
          ))}
        </datalist>

        <div className="opening-balance-account-scope" aria-label="Opening balance posting account scope">
          {openingBalanceScopeItems.map((item) => (
            <span className={item.tone} key={item.label} title={item.detail}>
              <small>{item.label}</small>
              <strong>{item.value}</strong>
            </span>
          ))}
        </div>

        <div className="opening-balance-entry-grid">
          <div className="opening-balance-text-import">
            <label className="form-field wide">
              <span>Import text</span>
              <textarea
                value={importText}
                onChange={(event) => onImportTextChange(event.target.value)}
                disabled={isBusy}
                rows={5}
              />
            </label>
            <div className="opening-balance-import-actions">
              <label className="form-field">
                <span>Delimiter</span>
                <select
                  value={importDelimiter}
                  onChange={(event) => onImportDelimiterChange(event.target.value)}
                  disabled={isBusy}
                >
                  <option value="comma">Comma</option>
                  <option value="tab">Tab</option>
                  <option value="pipe">Pipe</option>
                </select>
              </label>
              <label className="form-field">
                <span>Format</span>
                <select
                  value={openingBalanceTemplateFormat}
                  onChange={(event) =>
                    setOpeningBalanceTemplateFormat(event.target.value as OpeningBalanceImportTemplateFormat)
                  }
                  disabled={isBusy}
                  title={getOpeningBalanceTemplateTitle(openingBalanceTemplateFormat)}
                >
                  {openingBalanceTemplateOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
              <button
                className="icon-button"
                type="button"
                onClick={() => onUseTemplate(openingBalanceTemplateFormat)}
                disabled={isBusy}
                title={getOpeningBalanceTemplateTitle(openingBalanceTemplateFormat)}
              >
                <ClipboardList size={16} />
                Template
              </button>
              <button
                className="icon-button primary"
                type="button"
                onClick={() => void onPreviewImportText()}
                disabled={isBusy || importText.trim() === ""}
                title="Parse opening balance import text"
              >
                <FileCheck2 size={16} />
                Parse
              </button>
            </div>
          </div>

          <div className="journal-line-grid opening-balance-line-grid">
            <div className="journal-line-head">Account code</div>
            <div className="journal-line-head">Debit</div>
            <div className="journal-line-head">Credit</div>
            <div className="journal-line-head">Description</div>
            <div className="journal-line-head">State</div>
            <div className="journal-line-head"> </div>
            {value.lines.map((line, index) => {
              const matchedAccount = findOpeningBalanceAccount(line.accountCode, postingAccounts);
              const hasAccountCode = line.accountCode.trim() !== "";
              const lineState = getOpeningBalanceLineState(line, matchedAccount);

              return (
                <div className={`journal-line-row ${lineState}`} key={`${index}-${line.accountCode}`}>
                  <label className="form-field journal-account-select">
                    <input
                      value={line.accountCode}
                      list={openingBalanceAccountListId}
                      onChange={(event) =>
                        updateOpeningBalanceLine(index, {
                          accountCode: event.target.value.toUpperCase()
                        })
                      }
                      disabled={isBusy}
                      placeholder="Posting account code"
                      title="Enter an active Detail/Subsidiary posting account code"
                    />
                    {matchedAccount !== null && (
                      <span className="journal-account-context">
                        <strong>{formatOpeningBalanceMatchedAccountLabel(matchedAccount)}</strong>
                        <small>{formatOpeningBalanceAccountContext(matchedAccount)}</small>
                      </span>
                    )}
                    {hasAccountCode && matchedAccount === null && (
                      <span className="journal-account-context warning">
                        <strong>Not matched</strong>
                        <small>Use an active Detail/Subsidiary posting account.</small>
                      </span>
                    )}
                  </label>
                  <label className="form-field">
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={line.debit}
                      onChange={(event) =>
                        updateOpeningBalanceLine(index, {
                          debit: event.target.value,
                          credit: event.target.value.trim() === "" ? line.credit : ""
                        })
                      }
                      disabled={isBusy}
                    />
                  </label>
                  <label className="form-field">
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={line.credit}
                      onChange={(event) =>
                        updateOpeningBalanceLine(index, {
                          credit: event.target.value,
                          debit: event.target.value.trim() === "" ? line.debit : ""
                        })
                      }
                      disabled={isBusy}
                    />
                  </label>
                  <label className="form-field">
                    <input
                      value={line.description}
                      onChange={(event) =>
                        updateOpeningBalanceLine(index, {
                          description: event.target.value
                        })
                      }
                      disabled={isBusy}
                    />
                  </label>
                  <span className={`opening-balance-line-state ${lineState}`}>
                    {openingBalanceLineStateLabel(lineState)}
                  </span>
                  <button
                    className="table-icon-button"
                    type="button"
                    onClick={() => removeOpeningBalanceLine(index)}
                    disabled={isBusy || value.lines.length <= 2}
                    title="Remove opening balance line"
                  >
                    <Trash2 size={14} />
                  </button>
                </div>
              );
            })}
          </div>
        </div>

        <div className="opening-balance-readiness-row" aria-label="Opening balance readiness">
          {[openingBalanceProfileReadiness, ...openingBalanceReadinessItems].map((item) => (
            <span className={item.tone} key={item.label} title={item.detail}>
              <small>{item.label}</small>
              <strong>{item.status}</strong>
            </span>
          ))}
        </div>

        <div className="journal-total-row">
          <div>
            <span>Debit</span>
            <strong>{formatMoney(openingTotalDebit)}</strong>
          </div>
          <div>
            <span>Credit</span>
            <strong>{formatMoney(openingTotalCredit)}</strong>
          </div>
          <div>
            <span>Difference</span>
            <strong>{formatMoney(openingDifference)}</strong>
          </div>
          <div className="journal-actions">
            <button
              className="icon-button"
              type="button"
              onClick={addOpeningBalanceLine}
              disabled={isBusy}
              title="Add opening balance line"
            >
              <Plus size={16} />
              Line
            </button>
            <button
              className="icon-button primary"
              type="submit"
              disabled={
                isBusy
                || value.entryDate.trim() === ""
                || value.currencyCode.trim() === ""
              }
              title="Validate opening balance import before posting"
            >
              <FileCheck2 size={16} />
              Preview
            </button>
            <button
              className="icon-button primary"
              type="button"
              onClick={() => void onPostOpeningBalance()}
              disabled={isBusy || !openingBalanceCanPost}
              title={openingBalancePostTitle}
            >
              <Send size={16} />
              Post
            </button>
          </div>
        </div>
      </form>
      {preview !== null && (
        <OpeningBalancePreviewDetail
          preview={preview}
          textPreview={textPreview}
        />
      )}
    </section>
  );
}

function OpeningBalancePreviewDetail({
  preview,
  textPreview
}: {
  preview: OpeningBalanceImportPreview;
  textPreview: OpeningBalanceImportTextPreview | null;
}) {
  return (
    <div className={`opening-balance-preview ${preview.canPost ? "ready" : "blocked"}`}>
      <div className="opening-balance-summary">
        <span>
          <small>Reference</small>
          <strong>{preview.sourceReference || "-"}</strong>
        </span>
        <span>
          <small>Valid lines</small>
          <strong>{preview.validLineCount}/{preview.importedLineCount}</strong>
        </span>
        <span>
          <small>Debit</small>
          <strong>{formatMoney(preview.totalDebit)}</strong>
        </span>
        <span>
          <small>Credit</small>
          <strong>{formatMoney(preview.totalCredit)}</strong>
        </span>
        <span>
          <small>Difference</small>
          <strong>{formatMoney(preview.difference)}</strong>
        </span>
      </div>
      {preview.blockers.length > 0 && (
        <div className="opening-balance-blockers">
          {preview.blockers.map((blocker) => (
            <span key={blocker}>{blocker}</span>
          ))}
        </div>
      )}
      {textPreview !== null && (
        <div className="opening-balance-text-summary">
          <span>
            <small>Format</small>
            <strong>{textPreview.format}</strong>
          </span>
          <span>
            <small>Parsed</small>
            <strong>{textPreview.parsedLineCount}</strong>
          </span>
          <span>
            <small>Ignored</small>
            <strong>{textPreview.ignoredLineCount}</strong>
          </span>
          <span>
            <small>Issues</small>
            <strong>{textPreview.parseIssues.length}</strong>
          </span>
        </div>
      )}
      {textPreview !== null && textPreview.parseIssues.length > 0 && (
        <div className="opening-balance-table-frame">
          <table className="opening-balance-table">
            <thead>
              <tr>
                <th>Line</th>
                <th>Column</th>
                <th>Value</th>
                <th>Issue</th>
              </tr>
            </thead>
            <tbody>
              {textPreview.parseIssues.map((issue) => (
                <tr key={`${issue.lineNumber}-${issue.column}-${issue.message}`}>
                  <td>{issue.lineNumber}</td>
                  <td>{issue.column}</td>
                  <td>{issue.rawValue ?? "-"}</td>
                  <td>{issue.message}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <div className="opening-balance-table-frame">
        <table className="opening-balance-table">
          <thead>
            <tr>
              <th>#</th>
              <th>Side</th>
              <th>Account</th>
              <th>Narration</th>
              <th>Debit</th>
              <th>Credit</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {preview.lines.map((line) => {
              const side = getOpeningBalancePreviewLineSide(line);

              return (
                <tr className={line.isValid ? side.toLowerCase() : "blocked"} key={`${line.lineNumber}-${line.accountCode}`}>
                  <td>{line.lineNumber}</td>
                  <td>
                    <span className={`opening-balance-line-state ${side.toLowerCase()}`}>{side}</span>
                  </td>
                  <td>
                    <span className="opening-balance-preview-account">
                      <strong>{line.accountCode || "-"}</strong>
                      <small>{formatOpeningBalancePreviewAccountMeta(line)}</small>
                    </span>
                  </td>
                  <td>{line.description?.trim() || line.ledgerAccountName || "-"}</td>
                  <td className="numeric">{formatMoney(line.debit)}</td>
                  <td className="numeric">{formatMoney(line.credit)}</td>
                  <td>
                    {line.isValid ? (
                      <span className="status-pill open">Valid</span>
                    ) : (
                      <span className="opening-balance-line-issues">
                        {line.issues.join(" ")}
                      </span>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
