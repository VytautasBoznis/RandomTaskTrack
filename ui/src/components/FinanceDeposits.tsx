import { useCallback, useEffect, useState } from 'react';
import { createDeposit, deleteDeposit, getFinanceOverview } from '../api';
import { useApiError } from '../hooks';
import { COMPOUNDING_LABELS } from '../types';
import type { Account, Compounding, Currency, Deposit } from '../types';

const today = () => new Date().toISOString().slice(0, 10);

/**
 * ISO dates compare as strings. A dated deposit that has not reached its date
 * is money in flight — the balance it came out of is already down by the
 * principal, and the account it lands in has not been credited yet.
 */
const statusOf = (deposit: Deposit, asOf: string) =>
  deposit.maturesOn === null ? 'Open-ended' : deposit.maturesOn > asOf ? 'Maturing' : 'Matured';

export default function FinanceDeposits({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [deposits, setDeposits] = useState<Deposit[] | null>(null);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [currencies, setCurrencies] = useState<Currency[]>([]);
  const [baseCurrency, setBaseCurrency] = useState('EUR');
  const [asOf, setAsOf] = useState(today());
  const [adding, setAdding] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);

  const reload = useCallback(
    () =>
      getFinanceOverview()
        .then((o) => {
          setDeposits(o.deposits);
          setAccounts(o.accounts);
          setCurrencies(o.currencies);
          setBaseCurrency(o.baseCurrency);
          setAsOf(o.today);
        })
        .catch(fail),
    [fail],
  );

  useEffect(() => {
    reload();
  }, [reload]);

  async function onDelete(deposit: Deposit) {
    if (
      !window.confirm(
        `Delete "${deposit.name}"? The principal goes back to the account it came out of.`,
      )
    ) {
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
        <p className="today">
          A deposit moves its own money: out of one account now, back into another with the
          interest on the day it matures.
        </p>
        <button type="button" onClick={() => setAdding(true)}>
          Add deposit
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {adding && (
        <DepositForm
          accounts={accounts}
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
            {deposits.map((deposit) => {
              const status = statusOf(deposit, asOf);
              const source = accounts.find((a) => a.id === deposit.sourceAccountId);
              const target = accounts.find((a) => a.id === deposit.targetAccountId);

              return (
                <li key={deposit.id}>
                  <span className="title">
                    {deposit.name}
                    <span className="kind">{status}</span>
                  </span>
                  <span className="notes">
                    {deposit.principal.toLocaleString()} {deposit.currency} at {deposit.annualRate}% ·{' '}
                    {COMPOUNDING_LABELS[deposit.compounding]} · opened {deposit.openedOn}
                    {deposit.maturesOn !== null &&
                      ` · ${status === 'Maturing' ? 'matures' : 'matured'} ${deposit.maturesOn}`}
                  </span>
                  <span className="notes">
                    {source === undefined
                      ? 'Not tied to an account — its transfer was logged by hand.'
                      : `Out of ${source.name}, back into ${target?.name ?? source.name}`}
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
              );
            })}
          </ul>
        </section>
      )}
    </>
  );
}

function DepositForm({
  accounts,
  currencies,
  baseCurrency,
  onSaved,
  onCancel,
  onError,
}: {
  accounts: Account[];
  currencies: Currency[];
  baseCurrency: string;
  onSaved: () => void;
  onCancel: () => void;
  onError: (e: unknown) => void;
}) {
  const [name, setName] = useState('');
  const [sourceAccountId, setSourceAccountId] = useState(accounts[0]?.id ?? '');

  // Empty means "same as the source", which is what most deposits do. Only
  // the ones that roll into a different pot need the second box.
  const [targetAccountId, setTargetAccountId] = useState('');
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
        sourceAccountId: sourceAccountId === '' ? null : sourceAccountId,
        targetAccountId:
          sourceAccountId === '' || targetAccountId === '' ? null : targetAccountId,
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
          Out of
          <select
            value={sourceAccountId}
            onChange={(e) => setSourceAccountId(e.target.value)}
          >
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.name}
              </option>
            ))}
            <option value="">Not tied to an account</option>
          </select>
        </label>

        {sourceAccountId !== '' && (
          <label>
            Back into
            <select value={targetAccountId} onChange={(e) => setTargetAccountId(e.target.value)}>
              <option value="">The same account</option>
              {accounts
                .filter((account) => account.id !== sourceAccountId)
                .map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.name}
                  </option>
                ))}
            </select>
          </label>
        )}
      </div>

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
