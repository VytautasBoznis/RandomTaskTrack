import { useCallback, useEffect, useState } from 'react';
import { createDeposit, deleteDeposit, getFinanceOverview } from '../api';
import { useApiError } from '../hooks';
import { COMPOUNDING_LABELS } from '../types';
import type { Compounding, Currency, Deposit } from '../types';

const today = () => new Date().toISOString().slice(0, 10);

export default function FinanceDeposits({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [deposits, setDeposits] = useState<Deposit[] | null>(null);
  const [currencies, setCurrencies] = useState<Currency[]>([]);
  const [baseCurrency, setBaseCurrency] = useState('EUR');
  const [adding, setAdding] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);

  const reload = useCallback(
    () =>
      getFinanceOverview()
        .then((o) => {
          setDeposits(o.deposits);
          setCurrencies(o.currencies);
          setBaseCurrency(o.baseCurrency);
        })
        .catch(fail),
    [fail],
  );

  useEffect(() => {
    reload();
  }, [reload]);

  async function onDelete(deposit: Deposit) {
    if (!window.confirm(`Delete "${deposit.name}"?`)) {
      return;
    }

    setBusyId(deposit.id);
    setError(null);

    try {
      await deleteDeposit(deposit.id);
      await reload();
    } catch (e) {
      fail(e);
    } finally {
      setBusyId(null);
    }
  }

  if (!deposits) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  return (
    <>
      <div className="toolbar">
        <p className="today">Growth here is contractual, so the projection values these exactly.</p>
        <button type="button" onClick={() => setAdding(true)}>
          Add deposit
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {adding && (
        <DepositForm
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

      {deposits.length === 0 ? (
        <p className="empty">No deposits.</p>
      ) : (
        <section className="card">
          <ul>
            {deposits.map((deposit) => (
              <li key={deposit.id}>
                <span className="title">{deposit.name}</span>
                <span className="notes">
                  {deposit.principal.toLocaleString()} {deposit.currency} at {deposit.annualRate}% ·{' '}
                  {COMPOUNDING_LABELS[deposit.compounding]} · opened {deposit.openedOn} ·{' '}
                  {deposit.maturesOn === null ? 'open-ended' : `matures ${deposit.maturesOn}`}
                </span>
                <span className="actions">
                  <button
                    type="button"
                    className="danger"
                    disabled={busyId === deposit.id}
                    onClick={() => onDelete(deposit)}
                  >
                    Delete
                  </button>
                </span>
              </li>
            ))}
          </ul>
        </section>
      )}
    </>
  );
}

function DepositForm({
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
  const [name, setName] = useState('');
  const [principal, setPrincipal] = useState('');
  const [currency, setCurrency] = useState(baseCurrency);
  const [annualRate, setAnnualRate] = useState('');
  const [compounding, setCompounding] = useState<Compounding>(3);
  const [openedOn, setOpenedOn] = useState(today());
  const [maturesOn, setMaturesOn] = useState('');
  const [saving, setSaving] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);

    try {
      await createDeposit({
        name,
        principal: Number(principal),
        currency,
        annualRate: Number(annualRate),
        compounding,
        openedOn,
        maturesOn: maturesOn === '' ? null : maturesOn,
        note: null,
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
          Name
          <input value={name} onChange={(e) => setName(e.target.value)} required maxLength={200} />
        </label>

        <label>
          Principal
          <input
            type="number"
            step="0.01"
            min="0.01"
            value={principal}
            onChange={(e) => setPrincipal(e.target.value)}
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
          {/* As the bank writes it: 4.25 means 4.25%. */}
          Rate, % a year
          <input
            type="number"
            step="0.01"
            min="0"
            max="100"
            value={annualRate}
            onChange={(e) => setAnnualRate(e.target.value)}
            required
          />
        </label>

        <label>
          Compounding
          <select
            value={compounding}
            onChange={(e) => setCompounding(Number(e.target.value) as Compounding)}
          >
            <option value={3}>Annual</option>
            <option value={2}>Monthly</option>
            <option value={1}>Simple</option>
          </select>
        </label>
      </div>

      <div className="row">
        <label>
          Opened
          <input type="date" value={openedOn} onChange={(e) => setOpenedOn(e.target.value)} required />
        </label>

        <label>
          Matures
          <input type="date" value={maturesOn} onChange={(e) => setMaturesOn(e.target.value)} />
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
