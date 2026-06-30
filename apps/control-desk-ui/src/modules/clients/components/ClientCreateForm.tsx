import { Plus } from "lucide-react";
import type { FormEvent } from "react";
import type { CreateClientInput } from "../types/clientTypes";

type ClientCreateFormProps = {
  value: CreateClientInput;
  isBusy: boolean;
  onChange: (value: CreateClientInput) => void;
  onSubmit: () => Promise<void>;
};

export function ClientCreateForm({
  value,
  isBusy,
  onChange,
  onSubmit
}: ClientCreateFormProps) {
  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSubmit();
  }

  return (
    <form className="client-create-form" onSubmit={handleSubmit}>
      <label className="form-field">
        <span>Code</span>
        <input
          value={value.code}
          onChange={(event) => onChange({ ...value, code: event.target.value })}
          disabled={isBusy}
        />
      </label>
      <label className="form-field">
        <span>Legal name</span>
        <input
          value={value.legalName}
          onChange={(event) => onChange({ ...value, legalName: event.target.value })}
          disabled={isBusy}
        />
      </label>
      <label className="form-field">
        <span>Display name</span>
        <input
          value={value.displayName}
          onChange={(event) => onChange({ ...value, displayName: event.target.value })}
          disabled={isBusy}
        />
      </label>
      <button className="icon-button primary" type="submit" disabled={isBusy} title="Create client">
        <Plus size={16} />
        Create
      </button>
    </form>
  );
}
