import { useCallback, useEffect, useState } from 'react';
import { createFlow, deleteFlow, getFinanceOverview, setFlowActive } from '../api';
import { useApiError } from '../hooks';
import { CADENCE_LABELS, FlowKinds } from '../types';
import type { Cadence, Currency, FinanceFlow, FlowKind } from '../types';

const today = () => new Date().toISOString().slice(0, 10);

export default function FinanceRecurring({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [flows, setFlows] = useState<FinanceFlow[] | null>(null);
  const [currencies, setCurrencies] = useState<Currency[]>([]);
  const [baseCurrency, setBaseCurrency] = useState('EUR');
  const [adding, setAdding] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);

  const reload = useCallback(
    () =>
      getFinanceOverview()
        .then((o) => {
          setFlows(o.flows);
          setCurrencies(o.currencies);
          setBaseCurrency(o.baseCurrency);
        })
        .catch(fail),
    [fail],
  );

  useEffect(() => {
    reload();
  }, [reload]);

  async function onDelete(flow: FinanceFlow) {
    if (!window.confirm(`Delete "${flow.name}"? Entries already logged from it are kept.`)) {
      return;
    }

    setBusyId(flow.id);
    setError(null);

    try {
      await deleteFlow(flow.id);
      await reload();
    } catch (e) {
      fail(e);
    } finally {
      setBusyId(null);
    }
  }

  async function onToggle(flow: FinanceFlow) {
    setBusyId(flow.id);
    setError(null);

    try {
      await setFlowActive(flow.id, !flow.isActive);
      await reload();
    } catch (e) {
      fail(e);
    } finally {
      setBusyId(null);
    }
  }

  if (!flows) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  const income = flows.filter((f) => f.kind === FlowKinds.Income);
  const expense = flows.filter((f) => f.kind === FlowKinds.Expense);

  return (
    <>
      <div className="toolbar">
        <p className="today">What is supposed to happen. What actually did goes in the Ledger.</p>
        <button type="button" onClick={() => setAdding(true)}>
          Add recurring
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {adding && (
        <FlowForm
          currencies={currencies}
          baseCurrency={baseCurrency}
          onSaved={() => {
            setAdding(false);
            reload();
          }}
          onCancel={() => setAdding(false)}
          onError={fail}
        />
      )}

      <FlowList title="Coming in" flows={income} busyId={busyId} onToggle={onToggle} onDelete={onDelete} />
      <FlowList title="Going out" flows={expense} busyId={busyId} onToggle={onToggle} onDelete={onDelete} />
    </>
  );
}

function FlowList({
  title,
  flows,
  busyId,
  onToggle,
  onDelete,
}: {
  title: string;
  flows: FinanceFlow[];
  busyId: string | null;
  onToggle: (flow: FinanceFlow) => void;
  onDelete: (flow: FinanceFlow) => void;
}) {
  return (
    <section className="card">
      <h2>{title}</h2>

      {flows.length === 0 ? (
        <p className="empty">Nothing yet.</p>
      ) : (
        <ul>
          {flows.map((flow) => (
            <li key={flow.id} className={flow.isActive ? undefined : 'paused'}>
              <span className="title">{flow.name}</span>
              <span className="notes">
                {flow.amount.toLocaleString()} {flow.currency} · {CADENCE_LABELS[flow.cadence]}
                {flow.dayOfMonth !== null && ` · day ${flow.dayOfMonth}`}
                {flow.category !== null && ` · ${flow.category}`}
                {!flow.isActive && ' · paused'}
              </span>
              <span className="actions">
                {/* Real buttons, not text links: this is read at arm's length
                    on a wall tablet. */}
                <button type="button" disabled={busyId === flow.id} onClick={() => onToggle(flow)}>
                  {flow.isActive ? 'Pause' : 'Resume'}
                </button>
                <button
                  type="button"
                  className="danger"
                  disabled={busyId === flow.id}
                  onClick={() => onDelete(flow)}
                >
                  Delete
                </button>
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function FlowForm({
  currencies,
  baseCurrency,
  onSaved,
  onCancel,
  onError,
}: {
  currencies: Currency[];
  baseCurrency: string;
  onSaved: () => void;
  onCancel: () => void;
  onError: (e: unknown) => void;
}) {
  const [kind, setKind] = useState<FlowKind>(FlowKinds.Expense);
  const [name, setName] = useState('');
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState(baseCurrency);
  const [cadence, setCadence] = useState<Cadence>(2);
  const [dayOfMonth, setDayOfMonth] = useState('');
  const [startsOn, setStartsOn] = useState(today());
  const [category, setCategory] = useState('');
  const [saving, setSaving] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);

    try {
      await createFlow({
        kind,
        name,
        amount: Number(amount),
        currency,
        cadence,
        dayOfMonth: dayOfMonth === '' ? null : Number(dayOfMonth),
        monthOfYear: null,
        startsOn,
        endsOn: null,
        category: category === '' ? null : category,
      });

      onSaved();
    } catch (e) {
      onError(e);
    } finally {
      setSaving(false);
    }
  }

  return (
    <form className="card task-form" onSubmit={submit}>
      <div className="row">
        <label>
          Kind
          <select value={kind} onChange={(e) => setKind(Number(e.target.value) as FlowKind)}>
            <option value={1}>Income</option>
            <option value={2}>Expense</option>
          </select>
        </label>

        <label>
          Name
          <input value={name} onChange={(e) => setName(e.target.value)} required maxLength={200} />
        </label>
      </div>

      <div className="row">
        <label>
          Amount
          <input
            type="number"
            step="0.01"
            min="0.01"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
          />
        </label>

        <label>
          Currency
          <select value={currency} onChange={(e) => setCurrency(e.target.value)}>
            {currencies.map((c) => (
              <option key={c.code} value={c.code}>
                {c.code}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="row">
        <label>
          How often
          <select value={cadence} onChange={(e) => setCadence(Number(e.target.value) as Cadence)}>
            <option value={1}>Weekly</option>
            <option value={2}>Monthly</option>
            <option value={3}>Quarterly</option>
            <option value={4}>Yearly</option>
          </select>
        </label>

        <label>
          Day of month
          <input
            type="number"
            min="1"
            max="31"
            value={dayOfMonth}
            onChange={(e) => setDayOfMonth(e.target.value)}
            placeholder="same as start"
          />
        </label>
      </div>

      <div className="row">
        <label>
          Starts on
          <input type="date" value={startsOn} onChange={(e) => setStartsOn(e.target.value)} required />
        </label>

        <label>
          Category
          <input value={category} onChange={(e) => setCategory(e.target.value)} maxLength={100} />
        </label>
      </div>

      <div className="actions">
        <button type="submit" disabled={saving}>
          {saving ? 'Saving…' : 'Save'}
        </button>
        <button type="button" className="ghost" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}
