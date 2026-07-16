import type { Client360Tab } from "../../client-360/pages/Client360Page";

export type ClientWorkQueueItem = {
  clientId: string;
  code: string;
  name: string;
  status: string;
  actionLabel: string;
  detail: string;
  tab: Client360Tab;
  tone: "ready" | "warning" | "neutral";
  priority: number;
};

export type ClientWorkQueueSummary = {
  totalCount: number;
  setupCount: number;
  billingCount: number;
  paymentsCount: number;
  accessCount: number;
  cloudCount: number;
  overviewCount: number;
};

export type ClientWorkQueuePage = {
  items: ClientWorkQueueItem[];
  pageSize: number;
  hasMore: boolean;
  nextCursor?: string | null;
  filteredCount: number;
  summary: ClientWorkQueueSummary;
};

export type ClientQueueFilter =
  | "all"
  | "setup"
  | "billing"
  | "payments"
  | "access"
  | "cloud"
  | "overview";

export type ClientQueueSort = "priority" | "client" | "action";

type ClientQueueSummaryItem = {
  key: string;
  label: string;
  value: number;
  detail: string;
  filter: ClientQueueFilter;
  tone: "normal" | "warning" | "ready";
};

export const clientQueueFilters: Array<{ key: ClientQueueFilter; label: string }> = [
  { key: "all", label: "All" },
  { key: "setup", label: "Setup" },
  { key: "billing", label: "Billing" },
  { key: "payments", label: "Payments" },
  { key: "access", label: "Access" },
  { key: "cloud", label: "Cloud" },
  { key: "overview", label: "Review" }
];

export const clientQueueSortOptions: Array<{ key: ClientQueueSort; label: string }> = [
  { key: "priority", label: "Priority" },
  { key: "client", label: "Client" },
  { key: "action", label: "Action" }
];

export function countQueueItems(
  summary: ClientWorkQueueSummary,
  filter: ClientQueueFilter
): number {
  if (filter === "all") {
    return summary.totalCount;
  }

  return summary[`${filter}Count` as Exclude<keyof ClientWorkQueueSummary, "totalCount">];
}

export function createQueueSummary(summary: ClientWorkQueueSummary): ClientQueueSummaryItem[] {
  const setupCount = summary.setupCount;
  const billingCount = summary.billingCount;
  const paymentCount = summary.paymentsCount;
  const accessCount = summary.accessCount;
  const cloudCount = summary.cloudCount;
  const reviewCount = summary.overviewCount;

  return [
    {
      key: "total",
      label: "Clients",
      value: summary.totalCount,
      detail: "in daily queue",
      filter: "all",
      tone: "normal"
    },
    {
      key: "blockers",
      label: "Blockers",
      value: setupCount,
      detail: "setup lane",
      filter: "setup",
      tone: setupCount > 0 ? "warning" : "ready"
    },
    {
      key: "money",
      label: "Money Work",
      value: billingCount + paymentCount,
      detail: `${billingCount} billing / ${paymentCount} payment`,
      filter: billingCount > 0 ? "billing" : "payments",
      tone: billingCount + paymentCount > 0 ? "warning" : "ready"
    },
    {
      key: "cloud",
      label: "Cloud Work",
      value: cloudCount,
      detail: "updates pending",
      filter: "cloud",
      tone: cloudCount > 0 ? "warning" : "ready"
    },
    {
      key: "review",
      label: "Review Ready",
      value: reviewCount + accessCount,
      detail: `${accessCount} access / ${reviewCount} review`,
      filter: accessCount > 0 ? "access" : "overview",
      tone: "ready"
    }
  ];
}

export function queueFilterLabel(filter: ClientQueueFilter): string {
  return clientQueueFilters.find((item) => item.key === filter)?.label ?? "selected";
}

export function queueRefreshStatus({
  filter,
  isLoading,
  visibleCount
}: {
  filter: ClientQueueFilter;
  isLoading: boolean;
  visibleCount: number;
}): string {
  const lane = filter === "all" ? "all lanes" : `${queueFilterLabel(filter)} lane`;

  if (isLoading) {
    return `Refreshing ${lane.toLowerCase()}`;
  }

  return `${visibleCount} client${visibleCount === 1 ? "" : "s"} in ${lane.toLowerCase()}`;
}

export function queueLastRefreshedLabel(value: string | null): string {
  if (value === null) {
    return "Not refreshed yet";
  }

  return `Last refreshed ${formatQueueTime(value)}`;
}

export function queueLoadingMessage(filter: ClientQueueFilter): string {
  return filter === "all"
    ? "Loading all client work"
    : `Loading ${queueFilterLabel(filter).toLowerCase()} work`;
}

export function queueEmptyMessage(filter: ClientQueueFilter, search: string): string {
  if (search.trim() !== "") {
    return "No clients match search";
  }

  return `No ${queueFilterLabel(filter).toLowerCase()} work`;
}

function formatQueueTime(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat(undefined, {
    hour: "numeric",
    minute: "2-digit"
  }).format(date);
}
