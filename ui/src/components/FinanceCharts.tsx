import { useState } from 'react';
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ReferenceDot,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import type { FinanceTarget, ProjectionPoint } from '../types';

/**
 * Categorical slots 1-5, stepped for a dark surface, assigned in fixed order
 * per chart — never cycled, never by rank. Validated as a set against this
 * app's card surface (#1e2127) on the adjacent pairlist the stack actually
 * puts on screen: worst CVD ΔE 8.4, worst normal-vision ΔE 19.3, all five
 * ≥ 3:1 contrast.
 *
 * Five is the ceiling here, and it is a measured one rather than a taste:
 * no sixth hue in the ramp clears the all-pairs floors against these five.
 * That is why net worth below is drawn in ink instead — see NET.
 */
const SERIES = {
  one: '#3987e5',
  two: '#d95926',
  three: '#199e70',
  four: '#c98500',
  five: '#d55181',
};

// Chrome stays in ink, never in a series colour — a coloured mark beside a
// label carries identity, the text does not.
const GRID = '#2c313a';
const AXIS = '#8b93a1';
const SURFACE = '#1e2127';

/**
 * Net worth is not a sixth category, it is the total of the other five, so it
 * does not take a categorical hue — it takes primary ink. Being the only line
 * on a chart of fills, its identity is carried by its form rather than its
 * colour, which is what keeps it legible to a reader who cannot separate the
 * hues at all. Distinct from AXIS, which the target rules use.
 */
const NET = '#e6e8ec';

const money = (value: number, currency: string) =>
  new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(value);

const monthLabel = (month: string) =>
  new Date(`${month}T00:00:00`).toLocaleDateString(undefined, { month: 'short', year: '2-digit' });

/** Recharts hands the payload back untyped; this is the shape we put in. */
type Row = ProjectionPoint & { label: string; owed: number };

/**
 * `absolute` names the series that are drawn below the axis to carry a
 * direction — expenses, money owed — and should still read as the amount they
 * are. By name rather than by a flag over the whole tooltip, because the net
 * worth line shares a chart with one of them and can legitimately be negative:
 * flipping it would turn "you are 12k under water" into "you have 12k".
 */
function ChartTooltip({
  active,
  payload,
  label,
  currency,
  absolute = [],
}: {
  active?: boolean;
  payload?: { name?: string; value?: number; color?: string }[];
  label?: string;
  currency: string;
  absolute?: string[];
}) {
  if (!active || !payload || payload.length === 0) {
    return null;
  }

  return (
    <div className="chart-tip">
      <p className="chart-tip-head">{label}</p>
      {payload.map((item) => {
        const value = item.value ?? 0;
        const flip = item.name !== undefined && absolute.includes(item.name);

        return (
          <p key={item.name}>
            <span className="chart-swatch" style={{ background: item.color }} />
            {item.name}
            <b>{money(flip ? Math.abs(value) : value, currency)}</b>
          </p>
        );
      })}
    </div>
  );
}

/**
 * Net worth, stacked into what it is made of. Forward only: valuing holdings in
 * the past would need historical prices the app does not store, so the actual
 * months carry no balances and the area simply starts at today rather than
 * drawing a plausible line from nothing.
 *
 * With a debt on the books this becomes a balance sheet: the things you own
 * stack above the axis, what you owe hangs below it, and the ink line across
 * the middle is the difference. Without one, none of that machinery is drawn —
 * the top of the stack is already net worth, and a line tracing it plus a
 * legend entry reading "Owed €0" would be two pieces of furniture earning
 * nothing.
 */
export function NetWorthChart({
  points,
  targets,
  currency,
}: {
  points: ProjectionPoint[];
  targets: FinanceTarget[];
  currency: string;
}) {
  const rows: Row[] = points
    .filter((p) => p.netWorth !== null)
    .map((p) => ({
      ...p,
      label: monthLabel(p.month),
      // Recharts stacks by sign, so the negation is what hangs the debt below
      // the axis. The tooltip un-negates it.
      owed: -(p.debts ?? 0),
    }));

  if (rows.length === 0) {
    return <p className="empty">Nothing to project yet — add some money first.</p>;
  }

  const hasAssets = rows.some((r) => (r.assets ?? 0) !== 0);
  const hasDebts = rows.some((r) => (r.debts ?? 0) !== 0);

  const first = rows[0];
  const last = rows[rows.length - 1];

  // A target only gets a dot if its date is inside the window; outside it, the
  // amount still draws a line so "100k" is visible even when it is years out.
  const inWindow = (target: FinanceTarget) =>
    target.targetOn !== null &&
    target.targetOn >= first.month &&
    target.targetOn <= last.month;

  const dotFor = (target: FinanceTarget) => {
    const month = `${target.targetOn!.slice(0, 7)}-01`;

    return rows.find((r) => r.month === month);
  };

  return (
    <ResponsiveContainer width="100%" height={300}>
      <AreaChart
        data={rows}
        margin={{ top: 8, right: 16, bottom: 0, left: 8 }}
        stackOffset="sign"
      >
        {/* Solid hairlines. Horizontal only — vertical rules add noise a time
            axis already carries. */}
        <CartesianGrid stroke={GRID} strokeWidth={1} vertical={false} />
        <XAxis dataKey="label" stroke={AXIS} tickLine={false} axisLine={{ stroke: GRID }} minTickGap={24} />
        <YAxis
          stroke={AXIS}
          tickLine={false}
          axisLine={false}
          width={72}
          tickFormatter={(v: number) => money(v, currency)}
        />
        <Tooltip content={<ChartTooltip currency={currency} absolute={['Owed']} />} />
        <Legend iconType="square" wrapperStyle={{ paddingTop: 8 }} />

        {/* The axis has to be visible once anything hangs below it: it is the
            line the two halves are read against. */}
        {hasDebts && <ReferenceLine y={0} stroke={GRID} strokeWidth={1} />}

        {/* 2px surface gap between stacked fills rather than a border drawn
            round each one. */}
        <Area
          type="monotone"
          dataKey="cash"
          name="Cash"
          stackId="net"
          stroke={SURFACE}
          strokeWidth={2}
          fill={SERIES.one}
          fillOpacity={0.85}
        />
        <Area
          type="monotone"
          dataKey="deposits"
          name="Deposits"
          stackId="net"
          stroke={SURFACE}
          strokeWidth={2}
          fill={SERIES.two}
          fillOpacity={0.85}
        />
        <Area
          type="monotone"
          dataKey="stocks"
          name="Holdings"
          stackId="net"
          stroke={SURFACE}
          strokeWidth={2}
          fill={SERIES.three}
          fillOpacity={0.85}
        />

        {hasAssets && (
          <Area
            type="monotone"
            dataKey="assets"
            name="Property"
            stackId="net"
            stroke={SURFACE}
            strokeWidth={2}
            fill={SERIES.four}
            fillOpacity={0.85}
          />
        )}

        {hasDebts && (
          <Area
            type="monotone"
            dataKey="owed"
            name="Owed"
            stackId="net"
            stroke={SURFACE}
            strokeWidth={2}
            fill={SERIES.five}
            fillOpacity={0.85}
          />
        )}

        {/* Drawn last so it sits over the fills. No dots: 360 markers on a
            30-year projection is a solid bar, not a series. */}
        {hasDebts && (
          <Area
            type="monotone"
            dataKey="netWorth"
            name="Net worth"
            stroke={NET}
            strokeWidth={2}
            fill="none"
            fillOpacity={0}
            dot={false}
            activeDot={{ r: 4, fill: NET, stroke: SURFACE, strokeWidth: 2 }}
          />
        )}

        {targets
          .filter((t) => t.amount !== null)
          .map((t) => (
            <ReferenceLine
              key={`line-${t.id}`}
              y={t.amount!}
              stroke={AXIS}
              strokeWidth={1}
              label={{ value: t.label, position: 'insideTopLeft', fill: AXIS, fontSize: 12 }}
            />
          ))}

        {targets.filter(inWindow).map((t) => {
          const row = dotFor(t);

          return row === undefined ? null : (
            <ReferenceDot
              key={`dot-${t.id}`}
              x={row.label}
              y={t.amount ?? row.netWorth ?? 0}
              r={5}
              fill={AXIS}
              // 2px surface ring, not a border, on the overlapping marker.
              stroke={SURFACE}
              strokeWidth={2}
            />
          );
        })}
      </AreaChart>
    </ResponsiveContainer>
  );
}

/**
 * Money in against money out, per month. Expenses are drawn downward so the
 * direction carries the polarity and the colours stay plain identity — no
 * status red, which is reserved for things that are actually wrong.
 *
 * The dashed rule at today is the one place dashing earns its keep: it marks a
 * real boundary between logged history and projection, rather than decorating a
 * gridline.
 */
export function CashFlowChart({ points, currency }: { points: ProjectionPoint[]; currency: string }) {
  const rows = points.map((p) => ({
    ...p,
    label: monthLabel(p.month),
    // Recharts stacks by sign, so the negation is what puts expenses below the
    // axis. The tooltip un-negates it.
    outgoing: -p.expenses,
  }));

  if (rows.length === 0) {
    return <p className="empty">No months to show.</p>;
  }

  const firstProjected = rows.find((r) => !r.isActual);

  return (
    <ResponsiveContainer width="100%" height={260}>
      <BarChart data={rows} margin={{ top: 8, right: 16, bottom: 0, left: 8 }} stackOffset="sign">
        <CartesianGrid stroke={GRID} strokeWidth={1} vertical={false} />
        <XAxis dataKey="label" stroke={AXIS} tickLine={false} axisLine={{ stroke: GRID }} minTickGap={24} />
        <YAxis
          stroke={AXIS}
          tickLine={false}
          axisLine={false}
          width={72}
          tickFormatter={(v: number) => money(Math.abs(v), currency)}
        />
        <Tooltip
          cursor={{ fill: 'rgba(255,255,255,0.04)' }}
          content={<ChartTooltip currency={currency} absolute={['Out']} />}
        />
        <Legend iconType="square" wrapperStyle={{ paddingTop: 8 }} />
        <ReferenceLine y={0} stroke={GRID} strokeWidth={1} />

        {firstProjected && (
          <ReferenceLine
            x={firstProjected.label}
            stroke={AXIS}
            strokeWidth={1}
            strokeDasharray="4 4"
            label={{ value: 'projected', position: 'insideTopRight', fill: AXIS, fontSize: 12 }}
          />
        )}

        <Bar dataKey="income" name="In" stackId="flow" fill={SERIES.one} radius={[4, 4, 0, 0]} />
        <Bar dataKey="outgoing" name="Out" stackId="flow" fill={SERIES.two} radius={[0, 0, 4, 4]} />
      </BarChart>
    </ResponsiveContainer>
  );
}

/**
 * The table twin. Every chart here has one, so no value is reachable only by
 * hovering — which is also what makes the numbers readable on a tablet where
 * hover is a guess.
 */
export function ProjectionTable({ points, currency }: { points: ProjectionPoint[]; currency: string }) {
  const [open, setOpen] = useState(false);

  // Two more columns only when there is something to put in them. Nine columns
  // of which two read €0 all the way down is what makes a table on a tablet
  // scroll sideways for nothing.
  const hasAssets = points.some((p) => (p.assets ?? 0) !== 0);
  const hasDebts = points.some((p) => (p.debts ?? 0) !== 0);

  return (
    <>
      <button type="button" className="ghost" onClick={() => setOpen(!open)}>
        {open ? 'Hide the numbers' : 'Show the numbers'}
      </button>

      {open && (
        <div className="table-scroll">
          <table className="fin-table">
            <thead>
              <tr>
                <th>Month</th>
                <th>In</th>
                <th>Out</th>
                <th>Cash</th>
                <th>Deposits</th>
                <th>Holdings</th>
                {hasAssets && <th>Property</th>}
                {hasDebts && <th>Owed</th>}
                <th>Net worth</th>
              </tr>
            </thead>
            <tbody>
              {points.map((p) => (
                <tr key={p.month} className={p.isActual ? 'actual' : undefined}>
                  <td>
                    {monthLabel(p.month)}
                    {p.isActual && <span className="tag">actual</span>}
                  </td>
                  <td>{money(p.income, currency)}</td>
                  <td>{money(p.expenses, currency)}</td>
                  <td>{p.cash === null ? '—' : money(p.cash, currency)}</td>
                  <td>{p.deposits === null ? '—' : money(p.deposits, currency)}</td>
                  <td>{p.stocks === null ? '—' : money(p.stocks, currency)}</td>
                  {hasAssets && <td>{p.assets === null ? '—' : money(p.assets, currency)}</td>}
                  {hasDebts && <td>{p.debts === null ? '—' : money(p.debts, currency)}</td>}
                  <td>{p.netWorth === null ? '—' : money(p.netWorth, currency)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
