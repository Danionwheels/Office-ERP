import {
  AlertCircle,
  ArrowRight,
  Banknote,
  Building2,
  CheckCircle2,
  Cloud,
  ListChecks,
  MoveRight,
  RefreshCw,
  SlidersHorizontal
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { ApiError } from "../../../shared/api/apiError";
import type { Client360Tab } from "../../client-360/pages/Client360Page";
import { listClientWorkQueuePage } from "../api/commandCenterApi";
import {
  clientQueueFilters,
  clientQueueSortOptions,
  countQueueItems,
  createQueueSummary,
  queueEmptyMessage,
  queueLastRefreshedLabel,
  queueLoadingMessage,
  queueRefreshStatus,
  type ClientQueueFilter,
  type ClientQueueSort,
  type ClientWorkQueueItem,
  type ClientWorkQueueSummary
} from "../utils/commandCenterQueueModel";

type CommandCenterSectionKey =
  | "command-center"
  | "setup"
  | "client-360"
  | "commercial"
  | "deployment-cloud"
  | "access-security";

type CommandCenterPageProps = {
  onOpenSection: (section: CommandCenterSectionKey) => void;
  onOpenClientAction: (target: CommandCenterClientActionTarget) => void;
};

export type CommandCenterClientActionTarget = {
  clientId: string;
  tab: Client360Tab;
  reason: string;
};

type FlowStep = {
  key: CommandCenterSectionKey;
  label: string;
  summary: string;
  Icon: LucideIcon;
};

const flowSteps: FlowStep[] = [
  {
    key: "setup",
    label: "Setup",
    summary: "Reusable definitions",
    Icon: SlidersHorizontal
  },
  {
    key: "client-360",
    label: "Client 360",
    summary: "One client context",
    Icon: Building2
  },
  {
    key: "commercial",
    label: "Commercial",
    summary: "Contract to cash",
    Icon: Banknote
  },
  {
    key: "deployment-cloud",
    label: "Deployment",
    summary: "Runtime support",
    Icon: Cloud
  },
];

const emptyQueueSummary: ClientWorkQueueSummary = {
  totalCount: 0,
  setupCount: 0,
  billingCount: 0,
  paymentsCount: 0,
  accessCount: 0,
  cloudCount: 0,
  overviewCount: 0
};

export function CommandCenterPage({
  onOpenSection,
  onOpenClientAction
}: CommandCenterPageProps) {
  const [clientQueue, setClientQueue] = useState<ClientWorkQueueItem[]>([]);
  const [activeQueueFilter, setActiveQueueFilter] = useState<ClientQueueFilter>("all");
  const [queueSearch, setQueueSearch] = useState("");
  const [appliedQueueSearch, setAppliedQueueSearch] = useState("");
  const [queueSort, setQueueSort] = useState<ClientQueueSort>("priority");
  const [queueSummary, setQueueSummary] = useState<ClientWorkQueueSummary>(emptyQueueSummary);
  const [queueFilteredCount, setQueueFilteredCount] = useState(0);
  const [queueNextCursor, setQueueNextCursor] = useState<string | null>(null);
  const [isLoadingQueue, setIsLoadingQueue] = useState(true);
  const [isLoadingOlderQueue, setIsLoadingOlderQueue] = useState(false);
  const [queueLastRefreshedAtUtc, setQueueLastRefreshedAtUtc] = useState<string | null>(null);
  const [queueError, setQueueError] = useState("");
  const queueRequestId = useRef(0);
  const queueSummaryCards = useMemo(
    () => createQueueSummary(queueSummary),
    [queueSummary]
  );

  useEffect(() => {
    const timeout = window.setTimeout(
      () => setAppliedQueueSearch(queueSearch.trim()),
      250
    );

    return () => window.clearTimeout(timeout);
  }, [queueSearch]);

  useEffect(() => {
    void loadClientQueue(false);
  }, [activeQueueFilter, appliedQueueSearch, queueSort]);

  async function loadClientQueue(append: boolean) {
    const requestId = ++queueRequestId.current;

    if (append) {
      setIsLoadingOlderQueue(true);
    } else {
      setIsLoadingQueue(true);
    }
    setQueueError("");

    try {
      const page = await listClientWorkQueuePage({
        lane: activeQueueFilter,
        search: appliedQueueSearch,
        sort: queueSort,
        take: 25,
        cursor: append ? queueNextCursor ?? undefined : undefined
      });

      if (requestId !== queueRequestId.current) {
        return;
      }

      setClientQueue((current) => append
        ? mergeQueueItems(current, page.items)
        : page.items);
      setQueueSummary(page.summary);
      setQueueFilteredCount(page.filteredCount);
      setQueueNextCursor(page.nextCursor ?? null);
      setQueueLastRefreshedAtUtc(new Date().toISOString());
    } catch (caughtError) {
      if (requestId !== queueRequestId.current) {
        return;
      }

      setQueueError(formatCommandCenterError(caughtError, "Client queue could not be loaded."));

      if (!append) {
        setClientQueue([]);
        setQueueSummary(emptyQueueSummary);
        setQueueFilteredCount(0);
        setQueueNextCursor(null);
      }
    } finally {
      if (requestId === queueRequestId.current) {
        setIsLoadingQueue(false);
        setIsLoadingOlderQueue(false);
      }
    }
  }

  return (
    <section className="command-center-workspace">
      <div className="command-center-hero">
        <div>
          <span>Daily start</span>
          <h2>Command Center</h2>
          <p>Start with the client work that needs attention, then open the right Client 360 step.</p>
        </div>
        <div className="command-center-badge">
          <CheckCircle2 size={16} />
          <span>Live queue</span>
        </div>
      </div>

      <section className="command-client-panel" aria-label="Client work queue">
        <div className="command-client-heading">
          <div>
            <span>Daily queue</span>
            <h3>Client Work</h3>
          </div>
          <button
            className="icon-button"
            disabled={isLoadingQueue}
            onClick={() => loadClientQueue(false)}
            type="button"
          >
            <RefreshCw size={16} />
            Refresh
          </button>
        </div>

        <div className="command-client-refresh-row" aria-live="polite">
          <span>
            {queueRefreshStatus({
              filter: activeQueueFilter,
              isLoading: isLoadingQueue,
              visibleCount: queueFilteredCount
            })}
          </span>
          <span>{queueLastRefreshedLabel(queueLastRefreshedAtUtc)}</span>
        </div>

        {queueError !== "" && (
          <div className="command-client-error" role="alert">
            <AlertCircle size={16} />
            {queueError}
          </div>
        )}

        <div className="command-client-summary" aria-label="Client queue summary">
          {queueSummaryCards.map((item) => (
            <button
              className={`command-client-summary-card ${item.tone}`}
              disabled={isLoadingQueue}
              key={item.key}
              onClick={() => setActiveQueueFilter(item.filter)}
              type="button"
            >
              <span>{item.label}</span>
              <strong>{isLoadingQueue ? "..." : item.value}</strong>
              <small>{item.detail}</small>
            </button>
          ))}
        </div>

        <div className="command-client-filters" aria-label="Client queue filters">
          {clientQueueFilters.map((filter) => {
            const count = countQueueItems(queueSummary, filter.key);
            const className = [
              activeQueueFilter === filter.key ? "active" : "",
              isLoadingQueue ? "loading" : ""
            ].filter(Boolean).join(" ");

            return (
              <button
                aria-pressed={activeQueueFilter === filter.key}
                className={className}
                key={filter.key}
                onClick={() => setActiveQueueFilter(filter.key)}
                type="button"
              >
                <span>{filter.label}</span>
                <strong>{isLoadingQueue ? "..." : count}</strong>
              </button>
            );
          })}
        </div>

        <div className="command-client-tools">
          <label className="form-field">
            <span>Find Client</span>
            <input
              placeholder="Name, code, action"
              value={queueSearch}
              onChange={(event) => setQueueSearch(event.target.value)}
            />
          </label>

          <label className="form-field">
            <span>Sort</span>
            <select
              value={queueSort}
              onChange={(event) => setQueueSort(event.target.value as ClientQueueSort)}
            >
              {clientQueueSortOptions.map((option) => (
                <option key={option.key} value={option.key}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
        </div>

        <div className="command-client-list">
          {isLoadingQueue && (
            <div className="command-client-empty">
              {queueLoadingMessage(activeQueueFilter)}
            </div>
          )}
          {!isLoadingQueue && queueFilteredCount === 0 && queueError === "" && (
              <div className="command-client-empty">
                {queueSummary.totalCount === 0
                  ? "No clients"
                  : queueEmptyMessage(activeQueueFilter, queueSearch)}
              </div>
            )}
          {!isLoadingQueue
            && clientQueue.map((item) => (
              <button
                className={`command-client-row ${item.tone}`}
                key={item.clientId}
                onClick={() =>
                  onOpenClientAction({
                    clientId: item.clientId,
                    tab: item.tab,
                    reason: item.actionLabel
                  })
                }
                type="button"
              >
                <span className="command-client-main">
                  <strong>{item.name}</strong>
                  <small>{item.code} / {item.detail}</small>
                </span>
                <span className={`status-pill ${item.status.toLowerCase()}`}>
                  {item.status}
                </span>
                <span className="command-client-action">
                  {item.actionLabel}
                  <MoveRight size={14} />
                </span>
              </button>
            ))}
          {!isLoadingQueue && queueNextCursor !== null && (
            <button
              className="icon-button command-client-load-more"
              disabled={isLoadingOlderQueue}
              onClick={() => loadClientQueue(true)}
              type="button"
            >
              <ListChecks size={16} />
              {isLoadingOlderQueue ? "Loading" : `Load more (${clientQueue.length} of ${queueFilteredCount})`}
            </button>
          )}
        </div>
      </section>

      <section className="command-flow-panel" aria-label="Operating flow">
        {flowSteps.map((step, index) => (
          <div className="command-flow-item" key={step.key}>
            <button
              onClick={() => onOpenSection(step.key)}
              title={step.label}
              type="button"
            >
              <step.Icon size={18} />
              <span>
                <strong>{step.label}</strong>
                <small>{step.summary}</small>
              </span>
            </button>
            {index < flowSteps.length - 1 && <ArrowRight size={16} />}
          </div>
        ))}
      </section>
    </section>
  );
}

function mergeQueueItems(
  current: ClientWorkQueueItem[],
  next: ClientWorkQueueItem[]
): ClientWorkQueueItem[] {
  const byClientId = new Map(current.map((item) => [item.clientId, item]));

  next.forEach((item) => byClientId.set(item.clientId, item));

  return [...byClientId.values()];
}

function formatCommandCenterError(caughtError: unknown, fallback: string): string {
  if (caughtError instanceof ApiError) {
    const details = caughtError.errors.map((error) => error.message).join(" ");
    return details === "" ? caughtError.message : details;
  }

  if (caughtError instanceof Error) {
    return caughtError.message;
  }

  return fallback;
}
