import { ArrowUpDown, RefreshCw, Search, Users } from "lucide-react";
import { useMemo, useState, type KeyboardEvent } from "react";
import type { ClientLookup } from "../types/clientTypes";

type ClientListPanelProps = {
  clients: ClientLookup[];
  selectedClientId: string;
  isBusy: boolean;
  onSelect: (clientId: string) => void;
  onRefresh: () => Promise<void>;
};

type ClientSortKey = "code" | "displayName" | "legalName" | "status";
type SortDirection = "asc" | "desc";

export function ClientListPanel({
  clients,
  selectedClientId,
  isBusy,
  onSelect,
  onRefresh
}: ClientListPanelProps) {
  const [searchText, setSearchText] = useState("");
  const [sortKey, setSortKey] = useState<ClientSortKey>("code");
  const [sortDirection, setSortDirection] = useState<SortDirection>("asc");

  const filteredClients = useMemo(() => {
    const search = searchText.trim().toLowerCase();
    const matchingClients = search === ""
      ? clients
      : clients.filter((client) =>
          `${client.code} ${client.legalName} ${client.displayName} ${client.status}`
            .toLowerCase()
            .includes(search)
        );

    return [...matchingClients].sort((left, right) => {
      const leftValue = left[sortKey].trim().toLowerCase();
      const rightValue = right[sortKey].trim().toLowerCase();
      const result = leftValue.localeCompare(rightValue, undefined, {
        numeric: true,
        sensitivity: "base"
      });

      return sortDirection === "asc" ? result : -result;
    });
  }, [clients, searchText, sortDirection, sortKey]);
  const selectedClient = useMemo(
    () => clients.find((client) => client.clientId === selectedClientId) ?? null,
    [clients, selectedClientId]
  );

  function handleSort(nextSortKey: ClientSortKey) {
    if (nextSortKey === sortKey) {
      setSortDirection((current) => current === "asc" ? "desc" : "asc");
      return;
    }

    setSortKey(nextSortKey);
    setSortDirection("asc");
  }

  function handleRowKeyDown(
    event: KeyboardEvent<HTMLTableRowElement>,
    clientId: string
  ) {
    if (event.key !== "Enter" && event.key !== " ") {
      return;
    }

    event.preventDefault();
    onSelect(clientId);
  }

  return (
    <section className="client-panel client-list-panel">
      <div className="client-panel-heading">
        <div>
          <span>Clients</span>
          <strong>Client register</strong>
          <em>{filteredClients.length} shown of {clients.length}</em>
        </div>
        <button className="icon-button" type="button" onClick={onRefresh} disabled={isBusy} title="Refresh clients">
          <RefreshCw size={16} />
          Refresh
        </button>
      </div>

      <div className="client-register-toolbar">
        <label className="client-search">
          <Search size={16} />
          <input
            value={searchText}
            onChange={(event) => setSearchText(event.target.value)}
            placeholder="Search code, name, or status"
          />
        </label>
        <div className="client-register-summary" aria-live="polite">
          <span>{selectedClient === null ? "No client selected" : `Selected ${selectedClient.code}`}</span>
        </div>
      </div>

      <div className="client-register-frame">
        {filteredClients.length === 0 && (
          <div className="client-empty-state">
            <Users size={18} />
            <span>No clients</span>
          </div>
        )}

        {filteredClients.length > 0 && (
          <table className="client-register-table">
            <thead>
              <tr>
                <th className="client-register-selected-column" scope="col">
                  Sel
                </th>
                <SortableHeader
                  label="Code"
                  sortKeyName="code"
                  activeSortKey={sortKey}
                  sortDirection={sortDirection}
                  onSort={handleSort}
                />
                <SortableHeader
                  label="Display name"
                  sortKeyName="displayName"
                  activeSortKey={sortKey}
                  sortDirection={sortDirection}
                  onSort={handleSort}
                />
                <SortableHeader
                  label="Legal name"
                  sortKeyName="legalName"
                  activeSortKey={sortKey}
                  sortDirection={sortDirection}
                  onSort={handleSort}
                />
                <SortableHeader
                  label="Status"
                  sortKeyName="status"
                  activeSortKey={sortKey}
                  sortDirection={sortDirection}
                  onSort={handleSort}
                />
              </tr>
            </thead>
            <tbody>
              {filteredClients.map((client) => {
                const isSelected = client.clientId === selectedClientId;

                return (
                  <tr
                    aria-selected={isSelected}
                    className={isSelected ? "selected" : ""}
                    key={client.clientId}
                    onClick={() => onSelect(client.clientId)}
                    onKeyDown={(event) => handleRowKeyDown(event, client.clientId)}
                    tabIndex={0}
                  >
                    <td className="client-register-selected-column">
                      <span aria-label={isSelected ? "Selected client" : "Not selected"} />
                    </td>
                    <td title={client.code}>
                      <strong>{client.code}</strong>
                    </td>
                    <td title={client.displayName}>{client.displayName}</td>
                    <td title={client.legalName}>{client.legalName}</td>
                    <td>
                      <span className={`status-pill ${client.status.toLowerCase()}`}>
                        {client.status}
                      </span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </section>
  );
}

type SortableHeaderProps = {
  label: string;
  sortKeyName: ClientSortKey;
  activeSortKey: ClientSortKey;
  sortDirection: SortDirection;
  onSort: (sortKey: ClientSortKey) => void;
};

function SortableHeader({
  label,
  sortKeyName,
  activeSortKey,
  sortDirection,
  onSort
}: SortableHeaderProps) {
  const isActive = sortKeyName === activeSortKey;

  return (
    <th scope="col">
      <button
        className={isActive ? "client-register-sort active" : "client-register-sort"}
        type="button"
        onClick={() => onSort(sortKeyName)}
      >
        <span>{label}</span>
        <small>{isActive ? sortDirection : ""}</small>
        <ArrowUpDown size={14} />
      </button>
    </th>
  );
}
