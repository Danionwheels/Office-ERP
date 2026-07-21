import type { ReportClientLookup } from "../types/reportTypes";
import { formatReportInteger } from "./reportFormatting";

export function selectedClientLabel(
  clientId: string,
  clients: ReportClientLookup[]
): string {
  if (clientId === "") {
    return "All clients";
  }

  const client = clients.find((item) => item.clientId === clientId);
  return client === undefined ? "Selected client" : `${client.code} · ${client.displayName}`;
}

export function reportCountLabel(loadedCount: number, filteredCount?: number): string {
  return filteredCount === undefined
    ? `${formatReportInteger(loadedCount)} rows`
    : `${formatReportInteger(loadedCount)} of ${formatReportInteger(filteredCount)} rows`;
}
