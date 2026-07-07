import { AlertTriangle, CheckCircle2, RefreshCw, ShieldCheck, Wrench } from "lucide-react";
import type {
  LedgerAccountReconciliation,
  LedgerAccountReconciliationIssue,
  LedgerAccountReconciliationItem,
  LedgerAccountRepairAction,
  LedgerAccountRepairPlan
} from "../types/accountingTypes";

type LedgerAccountReconciliationPanelProps = {
  reconciliation: LedgerAccountReconciliation | null;
  repairPlan: LedgerAccountRepairPlan | null;
  isBusy: boolean;
  onApplyRepairAction: (
    ledgerAccountId: string,
    action: LedgerAccountRepairAction
  ) => Promise<void>;
  onRefresh: () => Promise<void>;
};

type CoaReconciliationTone = "ready" | "warning" | "danger" | "neutral";

type CoaReconciliationFact = {
  label: string;
  value: string;
  tone: CoaReconciliationTone;
  title?: string;
};

type IssueGroup = {
  code: string;
  severity: string;
  count: number;
  tone: CoaReconciliationTone;
};

export function LedgerAccountReconciliationPanel({
  reconciliation,
  repairPlan,
  isBusy,
  onApplyRepairAction,
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
  const healthFacts = getCoaReconciliationFacts(reconciliation, repairPlan);
  const issueGroups = getTopIssueGroups(reconciliation);

  async function handleApplyRepairAction(
    item: LedgerAccountReconciliationItem,
    action: LedgerAccountRepairAction
  ) {
    if (!confirmRepairAction(item, action)) {
      return;
    }

    await onApplyRepairAction(item.ledgerAccountId, action);
  }

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

      <div className="coa-reconciliation-health-row">
        {healthFacts.map((fact) => (
          <span className={fact.tone} key={fact.label} title={fact.title}>
            <small>{fact.label}</small>
            <strong>{fact.value}</strong>
          </span>
        ))}
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
        <div className="coa-reconciliation-state pending">
          <ShieldCheck size={18} />
          <span>
            <strong>COA health check pending</strong>
            <small>Account structure, ranges, and repair guidance are awaiting refresh.</small>
          </span>
        </div>
      ) : !hasIssues ? (
        <div className="coa-reconciliation-state ok">
          <CheckCircle2 size={18} />
          <span>
            <strong>COA health check passed</strong>
            <small>{accountCount} accounts checked; no repair actions required.</small>
          </span>
        </div>
      ) : (
        <div className="coa-reconciliation-table-frame">
          {issueGroups.length > 0 && (
            <div className="coa-reconciliation-issue-strip">
              {issueGroups.map((group) => (
                <span
                  className={group.tone}
                  key={`${group.severity}-${group.code}`}
                  title={formatSeverityLabel(group.severity)}
                >
                  <small>{group.code}</small>
                  <strong>{group.count}</strong>
                </span>
              ))}
            </div>
          )}
          <table className="coa-reconciliation-table">
            <thead>
              <tr>
                <th>Account</th>
                <th>COA role</th>
                <th>Normal</th>
                <th>Range</th>
                <th>State</th>
                <th>Issues</th>
                <th>Repair plan</th>
              </tr>
            </thead>
            <tbody>
              {reconciliation.items.map((item) => {
                const repairActions = actionsByAccountId.get(item.ledgerAccountId) ?? [];
                const rowTone = getItemTone(item);
                const issueState = getIssueState(item, repairActions);

                return (
                  <tr className={`coa-reconciliation-row ${rowTone}`} key={item.ledgerAccountId}>
                    <td>
                      <div className="coa-reconciliation-account-cell">
                        <strong>{item.displayCode}</strong>
                        <small>{item.name}</small>
                        <em>{item.status}</em>
                      </div>
                    </td>
                    <td>
                      <div className="coa-reconciliation-role-cell">
                        <strong>{item.type}</strong>
                        <small>{item.level} / {item.isPostingAccount ? "Posting" : "Non-posting"}</small>
                      </div>
                    </td>
                    <td>
                      <span className="coa-reconciliation-normal">
                        {item.normalBalance}
                      </span>
                    </td>
                    <td>
                      <div className="coa-reconciliation-range-cell">
                        <strong>{item.rangeRole ?? "-"}</strong>
                        <small>{item.rangeDisplayName ?? "No range"}</small>
                      </div>
                    </td>
                    <td>
                      <span className={`coa-reconciliation-state-chip ${issueState.tone}`}>
                        {issueState.label}
                      </span>
                      <small>{repairActions.length} repair action{repairActions.length === 1 ? "" : "s"}</small>
                    </td>
                    <td>
                      <div className="coa-reconciliation-issues">
                        {item.issues.map((issue) => (
                          <span
                            className={`coa-reconciliation-issue ${normalizeSeverityClass(issue.severity)}`}
                            key={`${item.ledgerAccountId}-${issue.code}`}
                            title={formatSeverityLabel(issue.severity)}
                          >
                            <strong>{issue.code}</strong>
                            <small>{issue.message}</small>
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
                              className={[
                                "coa-repair-action",
                                action.isAutomatable ? "guided" : "review",
                                normalizeSeverityClass(action.severity)
                              ].join(" ")}
                              key={`${item.ledgerAccountId}-${action.issueCode}-${action.actionCode}`}
                            >
                              <header>
                                <strong>{action.title}</strong>
                                <span>{formatRepairMode(action)}</span>
                              </header>
                              <div className="coa-repair-action-meta">
                                <span>{action.issueCode}</span>
                                <span>{action.actionCode}</span>
                              </div>
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
                              {action.notes.length > 0 && (
                                <small>{formatRepairNotes(action)}</small>
                              )}
                              {canApplyRepairAction(action) && (
                                <footer className="coa-repair-action-footer">
                                  <button
                                    className="coa-repair-apply-button"
                                    type="button"
                                    onClick={() => void handleApplyRepairAction(item, action)}
                                    disabled={isBusy}
                                    title="Apply guided repair"
                                  >
                                    <Wrench size={13} />
                                    Apply
                                  </button>
                                </footer>
                              )}
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
        </div>
      )}
    </section>
  );
}

function getCoaReconciliationFacts(
  reconciliation: LedgerAccountReconciliation | null,
  repairPlan: LedgerAccountRepairPlan | null
): CoaReconciliationFact[] {
  const accountCount = reconciliation?.accountCount ?? 0;
  const issueCount = reconciliation?.issueCount ?? 0;
  const affectedCount = reconciliation?.items.length ?? 0;
  const severityCounts = getSeverityCounts(reconciliation);
  const actionCounts = getRepairActionCounts(repairPlan);

  return [
    {
      label: "COA state",
      value: reconciliation === null ? "Pending" : issueCount === 0 ? "Clean" : "Needs repair",
      tone: reconciliation === null ? "neutral" : issueCount === 0 ? "ready" : "warning"
    },
    {
      label: "Error issues",
      value: String(severityCounts.error),
      tone: severityCounts.error === 0 ? "ready" : "danger"
    },
    {
      label: "Warnings",
      value: String(severityCounts.warning),
      tone: severityCounts.warning === 0 ? "ready" : "warning"
    },
    {
      label: "Affected",
      value: formatAffectedShare(affectedCount, accountCount),
      tone: affectedCount === 0 ? "ready" : "warning"
    },
    {
      label: "Auto repairs",
      value: String(actionCounts.automatable),
      tone: actionCounts.automatable > 0 ? "warning" : "neutral"
    },
    {
      label: "Manual review",
      value: String(actionCounts.review),
      tone: actionCounts.review === 0 ? "ready" : "warning"
    }
  ];
}

function getSeverityCounts(reconciliation: LedgerAccountReconciliation | null) {
  const counts = {
    error: 0,
    warning: 0,
    other: 0
  };

  reconciliation?.items.forEach((item) => {
    item.issues.forEach((issue) => {
      const severity = normalizeSeverityClass(issue.severity);

      if (severity === "error") {
        counts.error += 1;
        return;
      }

      if (severity === "warning") {
        counts.warning += 1;
        return;
      }

      counts.other += 1;
    });
  });

  return counts;
}

function getRepairActionCounts(repairPlan: LedgerAccountRepairPlan | null) {
  const counts = {
    automatable: 0,
    review: 0
  };

  repairPlan?.items.forEach((item) => {
    item.actions.forEach((action) => {
      if (action.isAutomatable) {
        counts.automatable += 1;
      } else {
        counts.review += 1;
      }
    });
  });

  return counts;
}

function getTopIssueGroups(reconciliation: LedgerAccountReconciliation | null): IssueGroup[] {
  const groups = new Map<string, IssueGroup>();

  reconciliation?.items.forEach((item) => {
    item.issues.forEach((issue) => {
      const key = `${normalizeSeverityClass(issue.severity)}:${issue.code}`;
      const current = groups.get(key);

      if (current) {
        current.count += 1;
        return;
      }

      groups.set(key, {
        code: issue.code,
        severity: issue.severity,
        count: 1,
        tone: getIssueTone(issue.severity)
      });
    });
  });

  return Array.from(groups.values())
    .sort((left, right) =>
      getSeverityRank(right.severity) - getSeverityRank(left.severity)
      || right.count - left.count
      || left.code.localeCompare(right.code)
    )
    .slice(0, 5);
}

function getIssueState(
  item: LedgerAccountReconciliationItem,
  repairActions: LedgerAccountRepairAction[]
): { label: string; tone: CoaReconciliationTone } {
  const tone = getItemTone(item);

  if (repairActions.length === 0) {
    return {
      label: tone === "danger" ? "Manual fix" : "Review",
      tone
    };
  }

  if (repairActions.every((action) => action.isAutomatable)) {
    return {
      label: "Guided repair",
      tone
    };
  }

  return {
    label: "Mixed repair",
    tone
  };
}

function getItemTone(item: LedgerAccountReconciliationItem): CoaReconciliationTone {
  const highestSeverity = getHighestIssueSeverity(item.issues);

  if (normalizeSeverityClass(highestSeverity) === "error") {
    return "danger";
  }

  if (normalizeSeverityClass(highestSeverity) === "warning") {
    return "warning";
  }

  return "neutral";
}

function getHighestIssueSeverity(issues: LedgerAccountReconciliationIssue[]): string {
  return issues.reduce(
    (highest, issue) =>
      getSeverityRank(issue.severity) > getSeverityRank(highest) ? issue.severity : highest,
    ""
  );
}

function getIssueTone(severity: string): CoaReconciliationTone {
  const normalizedSeverity = normalizeSeverityClass(severity);

  if (normalizedSeverity === "error") {
    return "danger";
  }

  if (normalizedSeverity === "warning") {
    return "warning";
  }

  return "neutral";
}

function getSeverityRank(severity: string): number {
  const normalizedSeverity = normalizeSeverityClass(severity);

  if (normalizedSeverity === "error") {
    return 3;
  }

  if (normalizedSeverity === "warning") {
    return 2;
  }

  return 1;
}

function normalizeSeverityClass(severity: string): string {
  const normalizedSeverity = severity.trim().toLowerCase();

  if (["critical", "error", "blocked"].includes(normalizedSeverity)) {
    return "error";
  }

  if (["warning", "warn"].includes(normalizedSeverity)) {
    return "warning";
  }

  return "info";
}

function formatSeverityLabel(severity: string): string {
  const normalizedSeverity = normalizeSeverityClass(severity);

  if (normalizedSeverity === "error") {
    return "Error";
  }

  if (normalizedSeverity === "warning") {
    return "Warning";
  }

  return severity || "Issue";
}

function formatAffectedShare(affectedCount: number, accountCount: number): string {
  if (accountCount === 0) {
    return "0";
  }

  return `${affectedCount}/${accountCount}`;
}

function formatRepairMode(action: LedgerAccountRepairAction): string {
  return action.isAutomatable ? "Guided" : action.repairMode;
}

function formatRepairNotes(action: LedgerAccountRepairAction): string {
  return action.notes.length === 1 ? action.notes[0] : `${action.notes[0]} (+${action.notes.length - 1})`;
}

function canApplyRepairAction(action: LedgerAccountRepairAction): boolean {
  return action.isAutomatable && action.repairMode === "GuidedPostingFlagUpdate";
}

function confirmRepairAction(
  item: LedgerAccountReconciliationItem,
  action: LedgerAccountRepairAction
): boolean {
  if (typeof window === "undefined") {
    return true;
  }

  return window.confirm([
    `Apply repair to ${item.displayCode} / ${item.name}?`,
    action.title,
    `Current: ${action.currentValue ?? "-"}`,
    `Suggested: ${action.suggestedValue ?? "-"}`
  ].join("\n"));
}
