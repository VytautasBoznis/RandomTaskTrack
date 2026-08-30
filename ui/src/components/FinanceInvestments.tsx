import { useCallback, useEffect, useState } from 'react';
import {
  createDividend,
  createHolding,
  createTrade,
  deleteDividend,
  deleteHolding,
  deleteTrade,
  getFinanceOverview,
  refreshPrices,
} from '../api';
import { useApiError } from '../hooks';
import { AccountKinds, CADENCE_LABELS, TradeSides } from '../types';
import type { Account, Cadence, Currency, Dividend, Position, Trade, TradeSide } from '../types';

const today = () => new Date().toISOString().slice(0, 10);

export default function FinanceInvestments({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [positions, setPositions] = useState<Position[] | null>(null);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [dividends, setDividends] = useState<Dividend[]>([]);
  const [currencies, setCurrencies] = useState<Currency[]>([]);
  const [baseCurrency, setBaseCurrency] = useState('EUR');
  const [adding, setAdding] = useState<'holding' | 'dividend' | null>(null);
  const [tradingId, setTradingId] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);
  const [refreshNote, setRefreshNote] = useState<string | null>(null);

  const reload = useCallback(
    () =>
      getFinanceOverview()
        .then((o) => {
          setPositions(o.positions);
          setAccounts(o.accounts);
          setDividends(o.dividends);
          setCurrencies(o.currencies);
          setBaseCurrency(o.baseCurrency);
        })
        .catch(fail),
    [fail],
  );

  useEffect(() => {
    reload();
  }, [reload]);

  async function onRefresh() {
    setRefreshing(true);
    setError(null);
    setRefreshNote(null);

    try {
      const result = await refreshPrices();

      // A dead ticker is reported rather than thrown, so say which one instead
      // of leaving a stale price looking fresh.
      setRefreshNote(
        result.failed.length === 0
          ? `Updated ${result.updatedHoldings} prices and ${result.updatedCurrencies} rates.`
          : `Updated ${result.updatedHoldings} prices. No price for: ${result.failed.join(', ')}.`,
      );

      await reload();
    } catch (e) {
      fail(e);
    } finally {
      setRefreshing(false);
    }
  }

  async function onDeleteHolding(position: Position) {
    if (!window.confirm(`Delete ${position.symbol}? Its trades and dividends go with it.`)) {
      return;
    }

    setBusyId(position.id);
    setError(null);

    try {
      await deleteHolding(position.id);
      await reload();
    } catch (e) {
      fail(e);
    } finally {
      setBusyId(null);
    }
  }

  async function onDeleteTrade(trade: Trade) {
    if (!window.confirm('Delete this trade? The position is recalculated from what is left.')) {
      return;
    }

    setBusyId(trade.id);
    setError(null);

    try {
      await deleteTrade(trade.id);
      await reload();
    } catch (e) {
      fail(e);
    } finally {
      setBusyId(null);
    }
  }

  async function onDeleteDividend(dividend: Dividend) {
    setBusyId(dividend.id);
    setError(null);

    try {
      await deleteDividend(dividend.id);
      await reload();
    } catch (e) {
      fail(e);
    } finally {
      setBusyId(null);
    }
  }

  if (!positions) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  const stockAccounts = accounts.filter((a) => a.kind === AccountKinds.Stock);

  // Grouped by account rather than one flat list: the same symbol can be held
  // in two of them, and "how much is in the pension" is the question the
  // grouping answers.
  const groups = accounts
    .map((account) => ({ account, items: positions.filter((p) => p.accountId === account.id) }))
    .filter((group) => group.items.length > 0);

  return (
    <>
      <div className="toolbar">
        <p className="today">Symbols are the price source&rsquo;s: AAPL, ASML.AS.</p>
        <span className="actions">
          <button type="button" onClick={onRefresh} disabled={refreshing}>
            {refreshing ? 'Refreshing…' : 'Refresh prices'}
          </button>
          <button type="button" onClick={() => setAdding('holding')}>
            Add holding
          </button>
        </span>
      </div>

      {error && <p className="error">{error}</p>}
      {refreshNote && <p className="muted">{refreshNote}</p>}

      {adding === 'holding' &&
        (stockAccounts.length === 0 ? (
          <p className="empty">
            Shares are held in an account. Add one on the Accounts tab, set to hold stocks, then
            come back.
          </p>
        ) : (
          <HoldingForm
            accounts={stockAccounts}
            currencies={currencies}
            onSaved={() => {
              setAdding(null);
              reload();
            }}
            onCancel={() => setAdding(null)}
            onError={fail}
          />
        ))}

      {positions.length === 0 ? (
        <p className="empty">No holdings yet.</p>
      ) : (
        groups.map((group) => (
          <div key={group.account.id}>
            <div className="group-head">
              <h2>{group.account.name}</h2>
              <span className="muted">
                {group.account.holdingsBase.toLocaleString()} {baseCurrency}
              </span>
            </div>

            {group.items.map((position) => (
              <section key={position.id} className="card">
                <div className="note-head">
                  <h2>
                    {position.symbol}
                    {position.name !== null && <span className="muted"> · {position.name}</span>}
                  </h2>
                  <span className="actions">
                    <button type="button" onClick={() => setTradingId(position.id)}>
                      Add trade
                    </button>
                    <button
                      type="button"
                      className="danger"
                      disabled={busyId === position.id}
                      onClick={() => onDeleteHolding(position)}
                    >
                      Delete
                    </button>
                  </span>
                </div>

                <p className="notes">
                  {position.quantity.toLocaleString()} shares · cost {position.costBasis.toLocaleString()}{' '}
                  {position.currency} ·{' '}
                  {position.lastPrice === null
                    ? 'no price yet'
                    : `${position.lastPrice.toLocaleString()} ${position.currency} each`}
                  {position.marketValueBase !== null &&
                    ` · worth ${position.marketValueBase.toLocaleString()} ${baseCurrency}`}
                </p>

                {tradingId === position.id && (
                  <TradeForm
                    holdingId={position.id}
                    onSaved={() => {
                      setTradingId(null);
                      reload();
                    }}
                    onCancel={() => setTradingId(null)}
                    onError={fail}
                  />
                )}

                {position.trades.length === 0 ? (
                  <p className="empty">No trades — the position is zero until you add one.</p>
                ) : (
                  <ul>
                    {position.trades.map((trade) => (
                      <li key={trade.id}>
                        <span className="title">
                          {trade.side === TradeSides.Buy ? 'Bought' : 'Sold'} {trade.quantity.toLocaleString()}
                        </span>
                        <span className="notes">
                          at {trade.price.toLocaleString()} {position.currency} · {trade.tradedOn}
                          {trade.fee > 0 && ` · fee ${trade.fee.toLocaleString()}`}
                        </span>
                        <span className="actions">
                          <button
                            type="button"
                            className="danger"
                            disabled={busyId === trade.id}
                            onClick={() => onDeleteTrade(trade)}
                          >
                            Delete
                          </button>
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            ))}
          </div>
        ))
      )}

      <section className="card">
        <div className="note-head">
          <h2>Expected dividends</h2>
          <button type="button" onClick={() => setAdding('dividend')}>
            Add dividend
          </button>
        </div>

        {adding === 'dividend' && (
          <DividendForm
            positions={positions}
            currencies={currencies}
            baseCurrency={baseCurrency}
            onSaved={() => {
              setAdding(null);
              reload();
            }}
            onCancel={() => setAdding(null)}
            onError={fail}
          />
        )}

        {dividends.length === 0 ? (
          <p className="empty">None. These feed the projection; a dividend that landed is a ledger entry.</p>
        ) : (
          <ul>
            {dividends.map((dividend) => (
              <li key={dividend.id}>
                <span className="title">{dividend.name}</span>
                <span className="notes">
                  {dividend.amount.toLocaleString()} {dividend.currency} ·{' '}
                  {CADENCE_LABELS[dividend.cadence]}
                </span>
                <span className="actions">
                  <button
                    type="button"
                    className="danger"
                    disabled={busyId === dividend.id}
                    onClick={() => onDeleteDividend(dividend)}
                  >
                    Delete
                  </button>
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </>
  );
}

function HoldingForm({
  accounts,
  currencies,
  onSaved,
  onCancel,
  onError,
}: {
  accounts: Account[];
  currencies: Currency[];
  onSaved: () => void;
  onCancel: () => void;
  onError: (e: unknown) => void;
}) {
  const [accountId, setAccountId] = useState(accounts[0].id);
  const [symbol, setSymbol] = useState('');
  const [name, setName] = useState('');
  const [currency, setCurrency] = useState('USD');
  const [saving, setSaving] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);

    try {
      await createHolding({ accountId, symbol, name: name === '' ? null : name, currency });
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
          Held in
          <select value={accountId} onChange={(e) => setAccountId(e.target.value)}>
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.name}
              </option>
            ))}
          </select>
        </label>

        <label>
          Symbol
          <input
            value={symbol}
            onChange={(e) => setSymbol(e.target.value)}
            required
            maxLength={40}
            placeholder="AAPL"
          />
        </label>

        <label>
          Name
          <input value={name} onChange={(e) => setName(e.target.value)} maxLength={200} />
        </label>

        <label>
          Quoted in
          <select value={currency} onChange={(e) => setCurrency(e.target.value)}>
            {currencies.map((c) => (
              <option key={c.code} value={c.code}>
                {c.code}
              </option>
            ))}
          </select>
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

function TradeForm({
  holdingId,
  onSaved,
  onCancel,
  onError,
}: {
  holdingId: string;
  onSaved: () => void;
  onCancel: () => void;
  onError: (e: unknown) => void;
}) {
  const [side, setSide] = useState<TradeSide>(TradeSides.Buy);
  const [quantity, setQuantity] = useState('');
  const [price, setPrice] = useState('');
  const [fee, setFee] = useState('');
  const [tradedOn, setTradedOn] = useState(today());
  const [saving, setSaving] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);

    try {
      await createTrade({
        holdingId,
        side,
        quantity: Number(quantity),
        price: Number(price),
        fee: fee === '' ? null : Number(fee),
        tradedOn,
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
    <form className="task-form" onSubmit={submit}>
      <div className="row">
        <label>
          Side
          <select value={side} onChange={(e) => setSide(Number(e.target.value) as TradeSide)}>
            <option value={1}>Buy</option>
            <option value={2}>Sell</option>
          </select>
        </label>

        <label>
          Shares
          <input
            type="number"
            step="0.000001"
            min="0"
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
            required
          />
        </label>

        <label>
          Price each
          <input
            type="number"
            step="0.0001"
            min="0"
            value={price}
            onChange={(e) => setPrice(e.target.value)}
            required
          />
        </label>
      </div>

      <div className="row">
        <label>
          Fee
          <input type="number" step="0.01" min="0" value={fee} onChange={(e) => setFee(e.target.value)} />
        </label>

        <label>
          Traded on
          <input type="date" value={tradedOn} onChange={(e) => setTradedOn(e.target.value)} required />
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

function DividendForm({
  positions,
  currencies,
  baseCurrency,
  onSaved,
  onCancel,
  onError,
}: {
  positions: Position[];
  currencies: Currency[];
  baseCurrency: string;
  onSaved: () => void;
  onCancel: () => void;
  onError: (e: unknown) => void;
}) {
  const [holdingId, setHoldingId] = useState('');
  const [name, setName] = useState('');
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState(baseCurrency);
  const [cadence, setCadence] = useState<Cadence>(3);
  const [startsOn, setStartsOn] = useState(today());
  const [saving, setSaving] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);

    try {
      await createDividend({
        holdingId: holdingId === '' ? null : holdingId,
        name,
        amount: Number(amount),
        currency,
        cadence,
        dayOfMonth: null,
        monthOfYear: null,
        startsOn,
        endsOn: null,
      });

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
          From
          <select value={holdingId} onChange={(e) => setHoldingId(e.target.value)}>
            <option value="">Not tracked as a holding</option>
            {positions.map((p) => (
              <option key={p.id} value={p.id}>
                {p.symbol}
              </option>
            ))}
          </select>
        </label>

        <label>
          Name
          <input value={name} onChange={(e) => setName(e.target.value)} required maxLength={200} />
        </label>
      </div>

      <div className="row">
        <label>
          Per payment
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

        <label>
          How often
          <select value={cadence} onChange={(e) => setCadence(Number(e.target.value) as Cadence)}>
            <option value={1}>Weekly</option>
            <option value={2}>Monthly</option>
            <option value={3}>Quarterly</option>
            <option value={4}>Yearly</option>
          </select>
        </label>
      </div>

      <div className="row">
        <label>
          First payment
          <input type="date" value={startsOn} onChange={(e) => setStartsOn(e.target.value)} required />
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
