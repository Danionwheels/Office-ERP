import { RefreshCw, Search, Users } from "lucide-react";
import { useMemo, useState } from "react";
import type { ClientLookup } from "../types/clientTypes";

type ClientListPanelProps = {
  clients: ClientLookup[];
  selectedClientId: string;
  isBusy: boolean;
  onSelect: (clientId: string) => void;
  onRefresh: () => Promise<void>;
};

export function ClientListPanel({
  clients,
  selectedClientId,
  isBusy,
  onSelect,
  onRefresh
}: ClientListPanelProps) {
  const [searchText, setSearchText] = useState("");

  const filteredClients = useMemo(() => {
    const search = searchText.trim().toLowerCase();

    if (search === "") {
      return clients;
    }

    return clients.filter((client) =>
      `${client.code} ${client.legalName} ${client.displayName} ${client.status}`
        .toLowerCase()
        .includes(search)
    );
  }, [clients, searchText]);

  return (
    <section className="client-panel client-list-panel">
      <div className="client-panel-heading">
        <div>
          <span>Clients</span>
          <strong>{clients.length}</strong>
        </div>
        <button className="icon-button" type="button" onClick={onRefresh} disabled={isBusy} title="Refresh clients">
          <RefreshCw size={16} />
          Refresh
        </button>
      </div>

      <label className="client-search">
        <Search size={16} />
        <input
          value={searchText}
          onChange={(event) => setSearchText(event.target.value)}
          placeholder="Search"
        />
      </label>

      <div className="client-list" role="list">
        {filteredClients.length === 0 && (
          <div className="client-empty-state">
            <Users size={18} />
            <span>No clients</span>
          </div>
        )}

        {filteredClients.map((client) => (
          <button
            className={`client-row${client.clientId === selectedClientId ? " selected" : ""}`}
            key={client.clientId}
            type="button"
            onClick={() => onSelect(client.clientId)}
          >
            <span className="client-row-code">{client.code}</span>
            <span className="client-row-name">{client.displayName}</span>
            <span className={`status-pill ${client.status.toLowerCase()}`}>{client.status}</span>
          </button>
        ))}
      </div>
    </section>
  );
}
