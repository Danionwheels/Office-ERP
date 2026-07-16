import { Download, Printer } from "lucide-react";
import type { ReactNode } from "react";
import { formatReportMoney } from "../utils/reportFormatting";

export function ReportHeading({
  kicker,
  title,
  rowLabel,
  onExport,
  onPrint,
  disabled,
  isBusy = false
}: {
  kicker: string;
  title: string;
  rowLabel: string;
  onExport: () => void | Promise<void>;
  onPrint: () => void | Promise<void>;
  disabled: boolean;
  isBusy?: boolean;
}) {
  return (
    <header className="client-panel-heading report-panel-heading">
      <div>
        <span>{kicker}</span>
        <h3>{title}</h3>
        <em>{rowLabel}</em>
      </div>
      <ReportActions
        onExport={onExport}
        onPrint={onPrint}
        disabled={disabled}
        isBusy={isBusy}
      />
    </header>
  );
}

export function ReportActions({
  onExport,
  onPrint,
  disabled,
  isBusy = false
}: {
  onExport: () => void | Promise<void>;
  onPrint: () => void | Promise<void>;
  disabled: boolean;
  isBusy?: boolean;
}) {
  return (
    <div className="report-actions report-no-print">
      <button
        className="icon-button"
        type="button"
        disabled={disabled || isBusy}
        onClick={() => void onExport()}
        title="Export the full filtered report to CSV"
      >
        <Download size={14} />
        CSV
      </button>
      <button
        className="icon-button"
        type="button"
        disabled={disabled || isBusy}
        onClick={() => void onPrint()}
        title="Print the full filtered report or save it as PDF"
      >
        <Printer size={14} />
        Print / PDF
      </button>
    </div>
  );
}

export function PrintMetadata({ detail }: { detail: string }) {
  return (
    <div className="report-print-metadata">
      <strong>SafarSuite Control Desk</strong>
      <span>{detail}</span>
      <small>Generated {new Date().toLocaleString()}</small>
    </div>
  );
}

export function ReportContentState({
  isBusy,
  error,
  isEmpty,
  emptyMessage,
  hasData,
  children
}: {
  isBusy: boolean;
  error?: string;
  isEmpty: boolean;
  emptyMessage: string;
  hasData: boolean;
  children: ReactNode;
}) {
  return (
    <>
      {isBusy && !hasData && <div className="report-feedback">Loading report…</div>}
      {error && <div className="report-feedback warning" role="alert">{error}</div>}
      {isEmpty ? <div className="report-empty-state">{emptyMessage}</div> : children}
    </>
  );
}

export function ReportSummary({
  items
}: {
  items: Array<{ label: string; value: string; tone?: "ready" | "warning" }>;
}) {
  return (
    <dl className="report-summary-grid">
      {items.map((item) => (
        <div className={item.tone ?? ""} key={item.label}>
          <dt>{item.label}</dt>
          <dd>{item.value}</dd>
        </div>
      ))}
    </dl>
  );
}

export function MoneyCell({
  value,
  currencyCode,
  strong = false
}: {
  value: number;
  currencyCode: string;
  strong?: boolean;
}) {
  return (
    <td className="numeric">
      {strong ? (
        <strong>{formatReportMoney(value, currencyCode)}</strong>
      ) : (
        formatReportMoney(value, currencyCode)
      )}
    </td>
  );
}

export function StatusPill({ value }: { value: string }) {
  return <span className={`status-pill ${statusTone(value)}`}>{humanizeToken(value)}</span>;
}

export function humanizeToken(value: string): string {
  return value
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/[_-]+/g, " ")
    .replace(/\b\w/g, (character) => character.toUpperCase());
}

function statusTone(value: string): string {
  const normalized = value.toLowerCase();

  if (["approved", "paid", "posted", "issued", "active"].includes(normalized)) {
    return "ready";
  }

  if (["rejected", "reversed", "void", "cancelled"].includes(normalized)) {
    return "danger";
  }

  return "warning";
}
