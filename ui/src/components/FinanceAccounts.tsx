import { useCallback, useEffect, useState } from 'react';
import {
  createAccount,
  deleteAccount,
  getFinanceOverview,
  setAccountBalance,
  updateAccount,
} from '../api';
import { useApiError } from '../hooks';
import { ACCOUNT_KIND_LABELS, AccountKinds } from '../types';
import type { Account, AccountKind, Currency } from '../types';

const money = (value: number, currency: string) =>
  new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(value);

/**
 * Where the money sits. The balance on each card is derived from the ledger and
 * the deposits, never stored — "Set balance" types the number the bank app
 * shows and logs the difference as an entry, so the total always has something
 * behind it.
 */
export default function FinanceAccounts({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [accounts, setAccounts] = useState<Account[] | null>(null);
  const [currencies, setCurrencies] = useState<Currency[]>([]);
  const [baseCurrency, setBaseCurrency] = useState('EUR');
  const [adding, setAdding] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [balancingId, setBalancingId] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const reload = useCallback(
    () =>
      getFinanceOverview()
        .then((o) => {
          setAccounts(o.accounts);
          setCurrencies(o.currencies);
          setBaseCurrency(o.baseCurrency);
        })
        .catch(fail),
    [fail],
  );

  useEffect(() => {
    reload();
  }, [reload]);

  async function onDelete(account: Account) {
    if (!window.confirm(`Delete "${account.name}"?`)) {
      return;
    }

    setBusyId(account.id);
    setError(null);

    try {
      await deleteAccount(account.id);
      await reload();
    } catch (e) {
      // An account with entries, holdings or deposits attached is refused with
      // a sentence saying how many. Nothing to do here but show it.
      fail(e);
    } finally {
      setBusyId(null);
    }
  }

  if (!accounts) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  return (
    <>
      <div className="toolbar">
        <p className="today">Balances come from the ledger, so they cannot drift.</p>
        <button type="button" onClick={() => setAdding(true)}>
          Add account
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {adding && (
        <AccountForm
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

      {accounts.length === 0 ? (
        <p className="empty">No accounts. Add one for each pot you want to see separately.</p>
      ) : (
        accounts.map((account) => (
          <section key={account.id} className="card">
            <div className="note-head">
              <h2>
                {account.name}
                <span className="kind">{ACCOUNT_KIND_LABELS[account.kind]}</span>
              </h2>
              <span className="actions">
                <button type="button" onClick={() => setBalancingId(account.id)}>
                  Set balance
                </button>
                <button type="button" className="ghost" onClick={() => setEditingId(account.id)}>
                  Edit
                </button>
                <button
                  type="button"
                  className="danger"
                  disabled={busyId === account.id}
                  onClick={() => onDelete(account)}
                >
                  Delete
                </button>
              </span>
            </div>

            <p className="figure">{money(account.valueBase, baseCurrency)}</p>

            {/* The breakdown only earns its line when there is something to
                break down — a plain bank account in the base currency would
                just be the same number twice. */}
            {account.holdingsBase !== 0 && (
              <p className="muted">
                {money(account.balanceBase, baseCurrency)} cash ·{' '}
                {money(account.holdingsBase, baseCurrency)} in shares
              </p>
            )}

            {account.currency !== baseCurrency && (
              <p className="muted">
                {money(account.balance, account.currency)} in the account&rsquo;s own currency
              </p>
            )}

            {account.maturingBase !== 0 && (
              <p className="muted">
                {money(account.maturingBase, baseCurrency)} maturing into this account
                {account.nextMaturityOn !== null && `, next on ${account.nextMaturityOn}`}
              </p>
            )}

            {account.note !== null && <p className="muted">{account.note}</p>}

            {balancingId === account.id && (
              <BalanceForm
                account={account}
                onSaved={() => {
                  setBalancingId(null);
                  reload();
                }}
                onCancel={() => setBalancingId(null)}
                onError={fail}
              />
            )}

            {editingId === account.id && (
              <AccountForm
                account={account}
                currencies={currencies}
                baseCurrency={baseCurrency}
                onSaved={() => {
                  setEditingId(null);
                  reload();
                }}
                onCancel={() => setEditingId(null)}
                onError={fail}
              />
            )}
          </section>
        ))
      )}
    </>
  );
}

/**
 * The number on the bank app, not a delta. The server works out the difference
 * and logs it, which is what keeps the balance derived.
 */
function BalanceForm({
  account,
  onSaved,
  onCancel,
  onError,
}: {
  account: Account;
  onSaved: () => void;
  onCancel: () => void;
  onError: (e: unknown) => void;
}) {
  const [balance, setBalance] = useState(String(account.balance));
  const [note, setNote] = useState('');
  const [saving, setSaving] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);

    try {
      await setAccountBalance(account.id, Number(balance), note === '' ? null : note);
      onSaved();
    } catch (e) {
      onError(e);
    } finally {
      setSaving(false);
    }
  }

  return (
    <form className="task-form" onSubmit={submit}>
      <div className="row">
        <label>
          What it should read, {account.currency}
          <input
            type="number"
            step="0.01"
            value={balance}
            onChange={(e) => setBalance(e.target.value)}
            required
          />
        </label>

        <label>
          Why
          <input
            value={note}
            onChange={(e) => setNote(e.target.value)}
            maxLength={500}
            placeholder="Optional — it lands in the ledger"
          />
        </label>
      </div>

      <p className="muted">
        The difference is logged as a &ldquo;Balance adjustment&rdquo; entry. A run of them means
        entries are being missed.
      </p>

      <div className="actions">
        <button type="submit" disabled={saving}>
          {saving ? 'Saving…' : 'Set balance'}
        </button>
        <button type="button" className="ghost" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}

function AccountForm({
  account,
  currencies,
  baseCurrency,
  onSaved,
  onCancel,
  onError,
}: {
  account?: Account;
  currencies: Currency[];
  baseCurrency: string;
  onSaved: () => void;
  onCancel: () => void;
  onError: (e: unknown) => void;
}) {
  const [name, setName] = useState(account?.name ?? '');
  const [kind, setKind] = useState<AccountKind>(account?.kind ?? AccountKinds.Cash);
  const [currency, setCurrency] = useState(account?.currency ?? baseCurrency);
  const [note, setNote] = useState(account?.note ?? '');
  const [saving, setSaving] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);

    const draft = { name, kind, currency, note: note === '' ? null : note };

    try {
      await (account ? updateAccount(account.id, draft) : createAccount(draft));
      onSaved();
    } catch (e) {
      onError(e);
    } finally {
      setSaving(false);
    }
  }

  return (
    <form className={account ? 'task-form' : 'card task-form'} onSubmit={submit}>
      <div className="row">
        <label>
          Name
          <input value={name} onChange={(e) => setName(e.target.value)} required maxLength={100} />
        </label>

        <label>
          Holds
          <select value={kind} onChange={(e) => setKind(Number(e.target.value) as AccountKind)}>
            <option value={1}>Cash — a bank account</option>
            <option value={2}>Stocks — a brokerage or pension</option>
          </select>
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
          Note
          <input value={note} onChange={(e) => setNote(e.target.value)} maxLength={500} />
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
