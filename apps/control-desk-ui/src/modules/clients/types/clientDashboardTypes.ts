import type { LucideIcon } from "lucide-react";

export type DashboardModule =
  | "dashboard"
  | "clients"
  | "profile"
  | "contracts"
  | "accounting"
  | "billing"
  | "payments"
  | "entitlements"
  | "cloud"
  | "statement";

export type BillingDashboardStep = "accounting" | "rules" | "draft" | "issue";

export type PaymentDashboardStep =
  | "readiness"
  | "cash"
  | "receipt"
  | "settlement"
  | "refund"
  | "result";

export type JournalSourceDocumentTarget =
  | { module: "billing"; step: BillingDashboardStep; label: string }
  | { module: "payments"; step: PaymentDashboardStep; label: string };

export type ModuleCommandItem = {
  key: string;
  label: string;
  title: string;
  Icon: LucideIcon;
  onClick?: () => void | Promise<void>;
  disabled?: boolean;
  variant?: "primary" | "default";
};

export type DashboardTone = "neutral" | "ready" | "warning";

export type DashboardMetric = {
  label: string;
  value: string;
  summary: string;
  tone: DashboardTone;
  Icon: LucideIcon;
  module: DashboardModule;
};

export type DashboardNavigationItem = {
  module: DashboardModule;
  label: string;
  summary: string;
  description: string;
  tone: DashboardTone;
  Icon: LucideIcon;
};

export type DashboardWorkQueuePriority = "high" | "medium" | "low" | "done";

export type DashboardWorkQueueItem = {
  key: string;
  priority: DashboardWorkQueuePriority;
  area: string;
  label: string;
  detail: string;
  status: string;
  actionLabel: string;
  module: DashboardModule;
  Icon: LucideIcon;
};
