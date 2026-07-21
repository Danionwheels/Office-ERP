import { RefreshCw } from "lucide-react";
import {
  useEffect,
  useId,
  useRef,
  useState,
  type FormEvent,
  type KeyboardEvent,
  type ReactNode
} from "react";
import type { ReportClientLookup } from "../types/reportTypes";
import { normalizeCurrencyCode } from "../utils/reportFormatting";

export function ReportFilterBar({
  children,
  isBusy,
  onSubmit
}: {
  children: ReactNode;
  isBusy: boolean;
  onSubmit: () => Promise<void>;
}) {
  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();
    void onSubmit();
  }

  return (
    <form className="client-panel report-filter-bar report-no-print" onSubmit={handleSubmit}>
      <div className="report-filter-fields">{children}</div>
      <button className="icon-button report-run-button" type="submit" disabled={isBusy}>
        <RefreshCw size={15} className={isBusy ? "spin" : ""} />
        {isBusy ? "Running…" : "Run report"}
      </button>
    </form>
  );
}

export function DateRangeFields({
  fromDate,
  toDate,
  onChange
}: {
  fromDate: string;
  toDate: string;
  onChange: (range: { fromDate: string; toDate: string }) => void;
}) {
  return (
    <>
      <label className="form-field">
        <span>From</span>
        <input
          type="date"
          required
          max={toDate}
          value={fromDate}
          onChange={(event) => onChange({ fromDate: event.target.value, toDate })}
        />
      </label>
      <label className="form-field">
        <span>To</span>
        <input
          type="date"
          required
          min={fromDate}
          value={toDate}
          onChange={(event) => onChange({ fromDate, toDate: event.target.value })}
        />
      </label>
    </>
  );
}

export function CurrencyField({
  value,
  onChange
}: {
  value: string;
  onChange: (currencyCode: string) => void;
}) {
  return (
    <label className="form-field report-currency-field">
      <span>Currency</span>
      <input
        type="text"
        required
        minLength={3}
        maxLength={3}
        pattern="[A-Z]{3}"
        title="Use a three-letter currency code such as PKR or USD."
        value={value}
        onChange={(event) => onChange(normalizeCurrencyCode(event.target.value))}
      />
    </label>
  );
}

export function ClientField({
  clients,
  value,
  isSearching,
  onChange,
  onSearch
}: {
  clients: ReportClientLookup[];
  value: string;
  isSearching: boolean;
  onChange: (clientId: string) => void;
  onSearch: (searchText: string) => Promise<void>;
}) {
  const controlId = useId();
  const [searchText, setSearchText] = useState("");
  const firstRender = useRef(true);
  const onSearchRef = useRef(onSearch);
  const debounceTimeoutRef = useRef<number | null>(null);

  useEffect(() => {
    onSearchRef.current = onSearch;
  }, [onSearch]);

  useEffect(() => {
    if (firstRender.current) {
      firstRender.current = false;
      return;
    }

    debounceTimeoutRef.current = window.setTimeout(() => {
      debounceTimeoutRef.current = null;
      void onSearchRef.current(searchText);
    }, 350);

    return () => {
      if (debounceTimeoutRef.current !== null) {
        window.clearTimeout(debounceTimeoutRef.current);
        debounceTimeoutRef.current = null;
      }
    };
  }, [searchText]);

  function handleSearchKeyDown(event: KeyboardEvent<HTMLInputElement>): void {
    if (event.key !== "Enter") {
      return;
    }

    event.preventDefault();

    if (debounceTimeoutRef.current !== null) {
      window.clearTimeout(debounceTimeoutRef.current);
      debounceTimeoutRef.current = null;
    }

    void onSearchRef.current(searchText);
  }

  return (
    <div className="report-client-search-control report-client-field">
      <label className="form-field">
        <span>Find client</span>
        <input
          type="search"
          value={searchText}
          maxLength={128}
          autoComplete="off"
          placeholder="Code or name"
          aria-controls={`${controlId}-select`}
          aria-describedby={`${controlId}-status`}
          onChange={(event) => setSearchText(event.target.value)}
          onKeyDown={handleSearchKeyDown}
        />
        <small id={`${controlId}-status`} aria-live="polite">
          {isSearching
            ? "Searching clients…"
            : `${clients.length} seen client option${clients.length === 1 ? "" : "s"}`}
        </small>
      </label>
      <label className="form-field">
        <span>Client</span>
        <select
          id={`${controlId}-select`}
          value={value}
          onChange={(event) => onChange(event.target.value)}
        >
          <option value="">All clients</option>
          {clients.map((client) => (
            <option key={client.clientId} value={client.clientId}>
              {client.code} — {client.displayName}
            </option>
          ))}
        </select>
      </label>
    </div>
  );
}
