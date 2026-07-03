import { AlertTriangle, CheckCircle2, RefreshCw, ShieldCheck, Wrench } from "lucide-react";
import type {
  LedgerAccountReconciliation,
  LedgerAccountRepairPlan
} from "../types/accountingTypes";

type LedgerAccountReconciliationPanelProps = {
  reconciliation: LedgerAccountReconciliation | null;
  repairPlan: LedgerAccountRepairPlan | null;
  isBusy: boolean;
  onRefresh: () => Promise<void>;
};

export function LedgerAccountReconciliationPanel({
  reconciliation,
  repairPlan,
  isBusy,
  onRefresh
}: LedgerAccountReconciliationPanelProps) {
  const issueCount = reconciliation?.issueCount ?? 0;
  const accountCount = reconciliation?.accountCount ?? 0;
  const affectedCount = reconciliation?.items.length ?? 0;
  const actionCount = repairPlan?.actionCount ?? 0;
  const hasIssues = issueCount > 0;
  const actionsByAccountId = new Map(
    repairPlan?.items.map((item) => [item.ledgerAccountId, item.actions]) ?? []
  );

  return (
    <section className="client-panel coa-reconciliation-panel">
      <div className="client-panel-heading">
        <div>
          <span>{reconciliation?.companyCode ?? "MAIN"}</span>
          <strong>COA reconciliation</strong>
        </div>
        <button
          className="icon-button"
          type="button"
          onClick={() => void onRefresh()}
          disabled={isBusy}
          title="Refresh COA reconciliation"
        >
          <RefreshCw size={16} />
          Refresh
        </button>
      </div>

      <div className="coa-reconciliation-summary">
        <article>
          <ShieldCheck size={17} />
          <span>Accounts checked</span>
          <strong>{accountCount}</strong>
        </article>
        <article className={hasIssues ? "attention" : "ok"}>
          {hasIssues ? <AlertTriangle size={17} /> : <CheckCircle2 size={17} />}
          <span>Issues</span>
          <strong>{issueCount}</strong>
        </article>
        <article>
          <AlertTriangle size={17} />
          <span>Affected accounts</span>
          <strong>{affectedCount}</strong>
        </article>
        <article>
          <Wrench size={17} />
          <span>Repair actions</span>
          <strong>{actionCount}</strong>
        </article>
      </div>

      {reconciliation === null ? (
        <div className="client-empty-state">Reconciliation has not loaded</div>
      ) : !hasIssues ? (
        <div className="client-empty-state">No COA reconciliation issues found</div>
      ) : (
        <table className="coa-reconciliation-table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Level</th>
              <th>Range</th>
              <th>Issues</th>
              <th>Repair plan</th>
            </tr>
          </thead>
          <tbody>
            {reconciliation.items.map((item) => {
              const repairActions = actionsByAccountId.get(item.ledgerAccountId) ?? [];

              return (
                <tr key={item.ledgerAccountId}>
                  <td>
                    <strong>{item.displayCode}</strong>
                    <small>{item.status}</small>
                  </td>
                  <td>{item.name}</td>
                  <td>
                    <span>{item.level}</span>
                    <small>{item.isPostingAccount ? "Posting" : "Non-posting"}</small>
                  </td>
                  <td>
                    <span>{item.rangeRole ?? "-"}</span>
                    <small>{item.rangeDisplayName ?? "No range"}</small>
                  </td>
                  <td>
                    <div className="coa-reconciliation-issues">
                      {item.issues.map((issue) => (
                        <span
                          className={`coa-reconciliation-issue ${issue.severity.toLowerCase()}`}
                          key={`${item.ledgerAccountId}-${issue.code}`}
                          title={issue.code}
                        >
                          {issue.message}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td>
                    <div className="coa-repair-actions">
                      {repairActions.length === 0 ? (
                        <span className="coa-repair-action empty">No dry-run action</span>
                      ) : (
                        repairActions.map((action) => (
                          <article
                            className={`coa-repair-action ${action.isAutomatable ? "guided" : "review"}`}
                            key={`${item.ledgerAccountId}-${action.issueCode}-${action.actionCode}`}
                          >
                            <header>
                              <strong>{action.title}</strong>
                              <span>{action.repairMode}</span>
                            </header>
                            <p>{action.description}</p>
                            {(action.currentValue ?? action.suggestedValue) && (
                              <dl>
                                <div>
                                  <dt>Current</dt>
                                  <dd>{action.currentValue ?? "-"}</dd>
                                </div>
                                <div>
                                  <dt>Suggested</dt>
                                  <dd>{action.suggestedValue ?? "-"}</dd>
                                </div>
                              </dl>
                            )}
                            {action.notes.length > 0 && <small>{action.notes[0]}</small>}
                          </article>
                        ))
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </section>
  );
}
