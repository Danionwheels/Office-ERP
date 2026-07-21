import { FileSearch, ListTree } from "lucide-react";
import type { ChartOfAccountsImportTextPreview } from "../../types/accountingTypes";
import { formatImportAction } from "../../utils/chartOfAccountsWorkspaceModel";

type ChartOfAccountsImportPanelProps = {
  importDelimiter: string;
  importPreview: ChartOfAccountsImportTextPreview | null;
  importPreviewStatus: string;
  importText: string;
  isBusy: boolean;
  onImportDelimiterChange: (value: string) => void;
  onImportTextChange: (value: string) => void;
  onPreviewImport: () => Promise<void>;
  onUseImportTemplate: () => void;
};

export function ChartOfAccountsImportPanel({
  importDelimiter,
  importPreview,
  importPreviewStatus,
  importText,
  isBusy,
  onImportDelimiterChange,
  onImportTextChange,
  onPreviewImport,
  onUseImportTemplate
}: ChartOfAccountsImportPanelProps) {
  return (
    <section className="entry-section coa-import-panel">
      <div className="section-heading-row">
        <h2>COA Import Preview</h2>
        <span>{importPreviewStatus}</span>
      </div>

      <div className="coa-import-editor">
        <label className="form-field">
          <span>Rows</span>
          <textarea
            value={importText}
            onChange={(event) => onImportTextChange(event.target.value)}
            disabled={isBusy}
            spellCheck={false}
          />
        </label>
        <div className="coa-import-actions">
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
          <button
            className="icon-button"
            type="button"
            onClick={onUseImportTemplate}
            disabled={isBusy}
            title="Load COA preview template"
          >
            <ListTree size={16} />
            Template
          </button>
          <button
            className="icon-button primary"
            type="button"
            onClick={() => void onPreviewImport()}
            disabled={isBusy || importText.trim() === ""}
            title="Preview chart of accounts import"
          >
            <FileSearch size={16} />
            Preview
          </button>
        </div>
      </div>

      {importPreview !== null && (
        <div className={`coa-import-preview ${importPreview.rejectCount === 0 ? "ready" : "blocked"}`}>
          <div className="coa-import-summary">
            <ImportPreviewFact label="Rows" value={importPreview.parsedLineCount.toString()} />
            <ImportPreviewFact label="Insert" value={importPreview.insertCount.toString()} />
            <ImportPreviewFact label="Update" value={importPreview.updateCount.toString()} />
            <ImportPreviewFact label="No change" value={importPreview.noChangeCount.toString()} />
            <ImportPreviewFact label="Reject" value={importPreview.rejectCount.toString()} />
            <ImportPreviewFact label="Warnings" value={importPreview.warningCount.toString()} />
          </div>

          {importPreview.parseIssues.length > 0 && (
            <div className="coa-import-issues">
              {importPreview.parseIssues.map((issue) => (
                <span key={`${issue.lineNumber}-${issue.column}-${issue.message}`}>
                  Line {issue.lineNumber} / {issue.column}: {issue.message}
                </span>
              ))}
            </div>
          )}

          <div className="coa-import-table-frame">
            <table className="coa-import-table">
              <thead>
                <tr>
                  <th>Line</th>
                  <th>Action</th>
                  <th>Acc Type</th>
                  <th>Acc Code</th>
                  <th>Account Name</th>
                  <th>Parent</th>
                  <th>Range</th>
                  <th>Issues</th>
                </tr>
              </thead>
              <tbody>
                {importPreview.rows.length === 0 ? (
                  <tr>
                    <td colSpan={8}>No rows parsed</td>
                  </tr>
                ) : (
                  importPreview.rows.map((row) => (
                    <tr
                      key={`${row.lineNumber}-${row.code}`}
                      className={`coa-import-row ${row.action.toLowerCase()}`}
                    >
                      <td>{row.lineNumber}</td>
                      <td>
                        <span className={`coa-import-action ${row.action.toLowerCase()}`}>
                          {formatImportAction(row.action)}
                        </span>
                      </td>
                      <td>{row.resolvedLevel}</td>
                      <td>
                        <strong>{row.displayCode}</strong>
                        <small>{row.type} / {row.normalBalance}</small>
                      </td>
                      <td title={row.name}>{row.name}</td>
                      <td>
                        {row.parentCode ?? "-"}
                        {row.parentSource !== null && row.parentSource !== undefined && (
                          <small>{row.parentSource}</small>
                        )}
                      </td>
                      <td>
                        {row.rangeRole ?? "-"}
                        {row.rangeDisplayName !== null && row.rangeDisplayName !== undefined && (
                          <small>{row.rangeDisplayName}</small>
                        )}
                      </td>
                      <td>
                        {row.issues.length === 0 ? (
                          <span className="coa-import-issue empty">Clear</span>
                        ) : (
                          <div className="coa-import-row-issues">
                            {row.issues.map((issue) => (
                              <span
                                className={`coa-import-issue ${issue.severity.toLowerCase()}`}
                                key={`${row.lineNumber}-${issue.code}-${issue.message}`}
                                title={issue.message}
                              >
                                {issue.code}
                              </span>
                            ))}
                          </div>
                        )}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </section>
  );
}

function ImportPreviewFact({
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
