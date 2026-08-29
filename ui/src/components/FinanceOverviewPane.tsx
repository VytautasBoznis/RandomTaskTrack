import { useCallback, useEffect, useState } from 'react';
import { getFinanceOverview, getProjection } from '../api';
import { useApiError } from '../hooks';
import { CashFlowChart, NetWorthChart, ProjectionTable } from './FinanceCharts';
import type { FinanceOverview, ProjectionPoint } from '../types';

const HORIZONS = [
  { months: 12, label: '1y' },
  { months: 60, label: '5y' },
  { months: 120, label: '10y' },
  { months: 360, label: '30y' },
];

// Kept on the tablet rather than in the database: it is a way of looking at the
// numbers, not a fact about them.
const GROWTH_KEY = 'rtt.finance.growth';
const HORIZON_KEY = 'rtt.finance.horizon';

const readStored = (key: string, fallback: number) => {
  const raw = Number(localStorage.getItem(key));

  return Number.isFinite(raw) && raw !== 0 ? raw : fallback;
};

export default function FinanceOverviewPane({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, fail } = useApiError(onUnauthorized);
  const [overview, setOverview] = useState<FinanceOverview | null>(null);
  const [points, setPoints] = useState<ProjectionPoint[] | null>(null);
  const [months, setMonths] = useState(() => readStored(HORIZON_KEY, 60));

  // Zero is a real choice here — hold the portfolio at its last price — so it
  // cannot use the falsy-fallback trick the horizon does.
  const [growth, setGrowth] = useState(() => {
    const raw = localStorage.getItem(GROWTH_KEY);

    return raw === null ? 0 : Number(raw);
  });

  const reload = useCallback(() => {
    getFinanceOverview().then(setOverview).catch(fail);
    getProjection(months, 12, growth).then(setPoints).catch(fail);
  }, [fail, months, growth]);

  useEffect(() => {
    reload();
  }, [reload]);

  const setHorizon = (value: number) => {
    localStorage.setItem(HORIZON_KEY, String(value));
    setMonths(value);
  };

  const setGrowthRate = (value: number) => {
    localStorage.setItem(GROWTH_KEY, String(value));
    setGrowth(value);
  };

  if (!overview) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  const currency = overview.baseCurrency;
  const money = (value: number) =>
    new Intl.NumberFormat(undefined, { style: 'currency', currency, maximumFractionDigits: 0 }).format(value);

  const monthlyNet = overview.monthlyIncomeBase - overview.monthlyExpenseBase;

  return (
    <>
      {/* One filter row above everything it scopes, not a control per card. */}
      <div className="toolbar">
        <div className="seg">
          {HORIZONS.map((h) => (
            <button
              key={h.months}
              type="button"
              className={h.months === months ? 'tab selected' : 'tab'}
              onClick={() => setHorizon(h.months)}
            >
              {h.label}
            </button>
          ))}
        </div>

        <label className="growth">
          Assumed return
          <input
            type="number"
            step="0.5"
            value={growth}
            onChange={(e) => setGrowthRate(Number(e.target.value))}
          />
          % a year
        </label>
      </div>

      {error && <p className="error">{error}</p>}

      {/* Stat tiles, not a chart: these are single numbers and a chart of one
          number is the commonest way to miss the point. */}
      <div className="tiles">
        <section className="card tile">
          <h2>Net worth</h2>
          <p className="figure">{money(overview.netWorthBase)}</p>
          {overview.hasUnpricedHoldings && (
            <p className="muted">Short by any holding with no price yet — press Refresh prices.</p>
          )}
        </section>

        <section className="card tile">
          <h2>Cash</h2>
          <p className="figure">{money(overview.cashBase)}</p>
          <p className="muted">From the ledger, not typed in.</p>
        </section>

        <section className="card tile">
          <h2>Deposits</h2>
          <p className="figure">{money(overview.depositsBase)}</p>
        </section>

        <section className="card tile">
          <h2>Holdings</h2>
          <p className="figure">{money(overview.stocksBase)}</p>
        </section>

        <section className="card tile">
          <h2>A typical month</h2>
          <p className="figure">{money(monthlyNet)}</p>
          <p className="muted">
            {money(overview.monthlyIncomeBase)} in, {money(overview.monthlyExpenseBase)} out
          </p>
        </section>
      </div>

      <section className="card">
        <h2>Net worth, projected</h2>
        {points === null ? (
          <p className="empty">Loading…</p>
        ) : (
          <>
            <NetWorthChart points={points} targets={overview.targets} currency={currency} />
            <p className="muted">
              {growth === 0
                ? 'Holdings held flat at their last price.'
                : `Holdings assumed to return ${growth}% a year.`}{' '}
              Deposits use their actual rate.
            </p>
          </>
        )}
      </section>

      <section className="card">
        <h2>Money in and out</h2>
        {points === null ? (
          <p className="empty">Loading…</p>
        ) : (
          <>
            <CashFlowChart points={points} currency={currency} />
            <p className="muted">Left of the dashed line is the ledger; right of it is the forecast.</p>
            <ProjectionTable points={points} currency={currency} />
          </>
        )}
      </section>
    </>
  );
}
