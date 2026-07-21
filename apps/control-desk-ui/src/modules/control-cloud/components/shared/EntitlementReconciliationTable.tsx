import type { ControlCloudEntitlementReconciliation } from "../../types/controlCloudTypes";

type EntitlementReconciliationTableProps = {
  reconciliation: ControlCloudEntitlementReconciliation | null;
};

export function EntitlementReconciliationTable({
  reconciliation
}: EntitlementReconciliationTableProps) {
  if (reconciliation === null) {
    return <div className="client360-empty compact">No reconciliation state</div>;
  }

  return (
    <div className="entitlement-reconciliation">
      <div className="entitlement-reconciliation-header">
        <div>
          <strong>Desired / Delivered / Observed</strong>
          <span>{reconciliation.detail}</span>
        </div>
        <span className={`reconciliation-state ${reconciliation.state.toLowerCase()}`}>
          {reconciliation.state}
        </span>
      </div>

      <dl className="entitlement-reconciliation-summary">
        <ReconciliationFact
          label="Desired"
          value={formatStateVersion(reconciliation.desired?.entitlementVersion)}
        />
        <ReconciliationFact
          label="Delivered"
          value={formatStateVersion(reconciliation.delivered?.entitlementVersion)}
        />
        <ReconciliationFact
          label="Observed"
          value={formatStateVersion(reconciliation.observed?.entitlementVersion)}
        />
        <ReconciliationFact
          label="Effective"
          value={formatDateTime(reconciliation.desired?.effectiveFromUtc ?? null)}
        />
      </dl>

      {reconciliation.differences.length === 0 ? (
        <div className="reconciliation-clean">All canonical access values match.</div>
      ) : (
        <div className="reconciliation-table-wrap">
          <table className="reconciliation-table">
            <thead>
              <tr>
                <th>Field</th>
                <th>Desired</th>
                <th>Delivered</th>
                <th>Observed</th>
                <th>State</th>
              </tr>
            </thead>
            <tbody>
              {reconciliation.differences.map((difference) => (
                <tr key={difference.field} title={difference.detail}>
                  <th scope="row">{formatFieldName(difference.field)}</th>
                  <td>{difference.desiredValue ?? "-"}</td>
                  <td>{difference.deliveredValue ?? "-"}</td>
                  <td>{difference.observedValue ?? "-"}</td>
                  <td>
                    <span className={`reconciliation-state compact ${difference.state.toLowerCase()}`}>
                      {difference.state}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function ReconciliationFact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}

function formatStateVersion(version: number | undefined): string {
  return version === undefined ? "Not reported" : `v${version}`;
}

function formatDateTime(value: string | null): string {
  if (value === null) {
    return "Not scheduled";
  }

  const parsed = new Date(value);

  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString();
}

function formatFieldName(value: string): string {
  return value.replace(/([a-z0-9])([A-Z])/g, "$1 $2");
}
