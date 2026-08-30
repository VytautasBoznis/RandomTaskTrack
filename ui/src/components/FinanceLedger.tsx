import { useCallback, useEffect, useState } from 'react';
import { createEntry, deleteEntry, getEntries, getFinanceOverview } from '../api';
import { useApiError } from '../hooks';
import { FlowKinds } from '../types';
import type { Account, Currency, FinanceFlow, FlowKind, LedgerEntry } from '../types';

const today = () => new Date().toISOString().slice(0, 10);

/**
 * The ledger is cash only — what actually moved. Current cash is derived from
 * it, so a balance that looks wrong means an entry is missing rather than a
 * number needing adjustment.
 */
export default function FinanceLedger({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [entries, setEntries] = useState<LedgerEntry[] | null>(null);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [flows, setFlows] = useState<FinanceFlow[]>([]);
  const [currencies, setCurrencies] = useState<Currency[]>([]);
  const [baseCurrency, setBaseCurrency] = useState('EUR');
  const [search, setSearch] = useState('');
  const [adding, setAdding] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);

  const reload = useCallback(
    () =>
      Promise.all([getEntries(null, null, search), getFinanceOverview()])
        .then(([rows, overview]) => {
          setEntries(rows);
          setAccounts(overview.accounts);
          setFlows(overview.flows);
          setCurrencies(overview.currencies);
          setBaseCurrency(overview.baseCurrency);
        })
        .catch(fail),
    [fail, search],
  );

  useEffect(() => {
    reload();
  }, [reload]);

  async function onDelete(entry: LedgerEntry) {
    if (!window.confirm(`Delete "${entry.name}"? This changes your cash balance.`)) {
      return;
    }

    setBusyId(entry.id);
    setError(null);

    try {
      await deleteEntry(entry.id);
      await reload();
    } catch (e) {
      fail(e);
    } finally {
      setBusyId(null);
    }
  }

  if (!entries) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  return (
    <>
      <div className="toolbar">
        <input
          className="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search the ledger"
        />
        <button type="button" onClick={() => setAdding(true)}>
          Log money
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {adding && (
        <EntryForm
          accounts={accounts}
          flows={flows}
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

      {entries.length === 0 ? (
        <p className="empty">
          Nothing logged. Start with one entry for what is in the account right now — call it
          &ldquo;Opening balance&rdquo;.
        </p>
      ) : (
        <section className="card">
          <ul>
            {entries.map((entry) => (
              <li key={entry.id}>
                <span className="title">{entry.name}</span>
                <span className="notes">
                  {entry.kind === FlowKinds.Income ? '+' : '−'}
                  {entry.amount.toLocaleString()} {entry.currency} · {entry.occurredOn}
                  {' · '}
                  {accounts.find((a) => a.id === entry.accountId)?.name ?? 'unknown account'}
                  {entry.category !== null && ` · ${entry.category}`}
                </span>
                <span className="actions">
                  <button
                    type="button"
                    className="danger"
                    disabled={busyId === entry.id}
                    onClick={() => onDelete(entry)}
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

function EntryForm({
  accounts,
  flows,
  currencies,
  baseCurrency,
  onSaved,
  onCancel,
  onError,
}: {
  accounts: Account[];
  flows: FinanceFlow[];
  currencies: Currency[];
  baseCurrency: string;
  onSaved: () => void;
  onCancel: () => void;
  onError: (e: unknown) => void;
}) {
  // The first account, which is the first cash one — the usual answer, and
  // still one tap to change.
  const [accountId, setAccountId] = useState(accounts[0]?.id ?? '');
  const [kind, setKind] = useState<FlowKind>(FlowKinds.Expense);
  const [name, setName] = useState('');
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState(baseCurrency);
  const [occurredOn, setOccurredOn] = useState(today());
  const [flowId, setFlowId] = useState('');
  const [category, setCategory] = useState('');
  const [saving, setSaving] = useState(false);

  // Picking the flow fills the rest in: most entries are an instance of
  // something already defined, and retyping it is how they stop getting logged.
  function pickFlow(id: string) {
    setFlowId(id);

    const flow = flows.find((f) => f.id === id);

    if (flow) {
      setKind(flow.kind);
      setName(flow.name);
      setAmount(String(flow.amount));
      setCurrency(flow.currency);
      setCategory(flow.category ?? '');
    }
  }

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);

    try {
      await createEntry({
        accountId,
        kind,
        name,
        amount: Number(amount),
        currency,
        occurredOn,
        flowId: flowId === '' ? null : flowId,
        category: category === '' ? null : category,
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
          Account
          <select value={accountId} onChange={(e) => setAccountId(e.target.value)} required>
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.name}
              </option>
            ))}
          </select>
        </label>

        <label>
          From a recurring one
          <select value={flowId} onChange={(e) => pickFlow(e.target.value)}>
            <option value="">One-off</option>
            {flows.map((flow) => (
              <option key={flow.id} value={flow.id}>
                {flow.name}
              </option>
            ))}
          </select>
        </label>

        <label>
          Kind
          <select value={kind} onChange={(e) => setKind(Number(e.target.value) as FlowKind)}>
            <option value={1}>Income</option>
            <option value={2}>Expense</option>
          </select>
        </label>
      </div>

      <div className="row">
        <label>
          Name
          <input value={name} onChange={(e) => setName(e.target.value)} required maxLength={200} />
        </label>

        <label>
          Happened on
          <input type="date" value={occurredOn} onChange={(e) => setOccurredOn(e.target.value)} required />
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
