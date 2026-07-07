import { Plus } from "lucide-react";
import type { FormEvent } from "react";
import type { AccountingPeriodFormInput } from "../../types/accountingTypes";

type AccountingPeriodCreatePanelProps = {
  value: AccountingPeriodFormInput;
  isBusy: boolean;
  canCreate: boolean;
  onValueChange: (value: AccountingPeriodFormInput) => void;
  onCreate: () => Promise<void>;
};

const accountingCompanyCode = "MAIN";

export function AccountingPeriodCreatePanel({
  value,
  isBusy,
  canCreate,
  onValueChange,
  onCreate
}: AccountingPeriodCreatePanelProps) {
  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onCreate();
  }

  return (
    <section className="client-panel accounting-period-form-panel">
      <div className="client-panel-heading">
        <div>
          <span>{accountingCompanyCode}</span>
          <strong>Period setup</strong>
        </div>
      </div>
      <form className="accounting-period-form" onSubmit={handleSubmit}>
        <label className="form-field">
          <span>Company</span>
          <input
            value={accountingCompanyCode}
            disabled
            readOnly
          />
        </label>
        <label className="form-field">
          <span>Name</span>
          <input
            value={value.name}
            onChange={(event) =>
              onValueChange({
                ...value,
                name: event.target.value
              })
            }
            disabled={isBusy}
          />
        </label>
        <label className="form-field">
          <span>Start</span>
          <input
            type="date"
            value={value.startsOn}
            onChange={(event) =>
              onValueChange({
                ...value,
                startsOn: event.target.value
              })
            }
            disabled={isBusy}
          />
        </label>
        <label className="form-field">
          <span>End</span>
          <input
            type="date"
            value={value.endsOn}
            onChange={(event) =>
              onValueChange({
                ...value,
                endsOn: event.target.value
              })
            }
            disabled={isBusy}
          />
        </label>
        <button
          className="icon-button primary"
          type="submit"
          disabled={isBusy || !canCreate}
          title="Create accounting period"
        >
          <Plus size={16} />
          Create
        </button>
      </form>
    </section>
  );
}
