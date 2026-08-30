import { useCallback, useEffect, useState } from 'react';
import {
  createDebt,
  createDebtPayment,
  deleteDebt,
  deleteDebtPayment,
  getFinanceOverview,
} from '../api';
import { useApiError } from '../hooks';
import type { Account, Currency, Debt } from '../types';

const today = () => new Date().toISOString().slice(0, 10);

/** ISO dates and ISO months both compare as strings. */
const monthOf = (date: string) => date.slice(0, 7);

const monthLabel = (month: string) =>
  new Date(`${month}-01T00:00:00`).toLocaleDateString(undefined, { month: 'short', year: 'numeric' });

/**
 * The gap between the contract and the projection, in words. This is the number
 * the overpayments are for, so it gets a sentence rather than a second date the
 * reader has to subtract themselves.
 */
const earlyBy = (endsOn: string | null, paidOffOn: string | null) => {
  if (endsOn === null || paidOffOn === null) {
    return null;
  }

  const end = new Date(`${monthOf(endsOn)}-01T00:00:00`);
  const off = new Date(`${monthOf(paidOffOn)}-01T00:00:00`);
  const months = (end.getFullYear() - off.getFullYear()) * 12 + end.getMonth() - off.getMonth();

  if (months <= 0) {
    return null;
  }

  const years = Math.floor(months / 12);
  const rest = months % 12;

  if (years === 0) {
    return `${rest} month${rest === 1 ? '' : 's'} early`;
  }

  return rest === 0
    ? `${years} year${years === 1 ? '' : 's'} early`
    : `${years}y ${rest}m early`;
};

/**
 * The monthly payment that clears a principal at a rate over a term — the
 * standard annuity formula, so the form can fill in the box rather than making
 * somebody find a mortgage calculator and type the answer back in.
 *
 * A zero rate is the degenerate case the formula divides by zero on, and it is
 * also the easy one: the payment is just the principal spread over the term.
 */
const annuityPayment = (principal: number, annualRate: number, months: number) => {
  if (months <= 0 || principal <= 0) {
    return null;
  }

  const monthly = annualRate / 100 / 12;

  if (monthly === 0) {
    return principal / months;
  }

  const growth = Math.pow(1 + monthly, months);

  return (principal * monthly * growth) / (growth - 1);
};

const monthsBetween = (from: string, to: string) => {
  const a = new Date(`${from}T00:00:00`);
  const b = new Date(`${to}T00:00:00`);

  return (b.getFullYear() - a.getFullYear()) * 12 + b.getMonth() - a.getMonth() + 1;
};

export default function FinanceDebts({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [debts, setDebts] = useState<Debt[] | null>(null);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [currencies, setCurrencies] = useState<Currency[]>([]);
  const [baseCurrency, setBaseCurrency] = useState('EUR');
  const [asOf, setAsOf] = useState(today());
  const [adding, setAdding] = useState(false);
  const [payingId, setPayingId] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const reload = useCallback(
    () =>
      getFinanceOverview()
        .then((o) => {
          setDebts(o.debts);
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

  async function onDelete(debt: Debt) {
    if (!window.confirm(`Delete "${debt.name}"? Its overpayments go with it.`)) {
      return;
    }

    setBusyId(debt.id);
    setError(null);

    try {
      await deleteDebt(debt.id);
      await reload();
    } catch (e) {
      fail(e);
    } finally {
      setBusyId(null);
    }
  }

  async function onDeletePayment(id: string) {
    if (!window.confirm('Remove this payment? The payoff date moves back out.')) {
      return;
    }

    setBusyId(id);
    setError(null);

    try {
      await deleteDebtPayment(id);
      await reload();
    } catch (e) {
      fail(e);
    } finally {
      setBusyId(null);
    }
  }

  if (!debts) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  const money = (value: number, currency: string) =>
    new Intl.NumberFormat(undefined, { style: 'currency', currency, maximumFractionDigits: 0 }).format(value);

  return (
    <>
      <div className="toolbar">
        <p className="today">
          A debt pays itself: the monthly payment shows up in the projection until the balance
          clears, so do not also add it as a recurring expense.
        </p>
        <button type="button" onClick={() => setAdding(true)}>
          Add debt
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {adding && (
        <DebtForm
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

      {debts.length === 0 ? (
        <p className="empty">Nothing owed.</p>
      ) : (
        debts.map((debt) => {
          // Nothing owed means one of two opposite things. A mortgage signed
          // next June owes nothing in March either, and calling that "Cleared"
          // would be the most cheerful wrong label on the page.
          const started = debt.startsOn <= asOf;
          const cleared = started && debt.outstanding === 0;
          const early = earlyBy(debt.endsOn, debt.paidOffOn);
          const downFrom = accounts.find((a) => a.id === debt.downPaymentAccountId);
          const landsIn = accounts.find((a) => a.id === debt.disbursesToAccountId);

          return (
            <section className="card" key={debt.id}>
              <h2>
                {debt.name}
                <span className="kind">
                  {!started
                    ? `Starts ${debt.startsOn}`
                    : cleared
                      ? 'Cleared'
                      : money(debt.outstanding, debt.currency)}
                </span>
              </h2>

              <p className="notes">
                {money(debt.principal, debt.currency)} borrowed at {debt.annualRate}% ·{' '}
                {money(debt.payment, debt.currency)} a month from {debt.startsOn}
              </p>

              {/* The two dates side by side, because the distance between them
                  is the entire argument for making an overpayment. */}
              <p className="notes">
                {debt.paidOffOn === null ? (
                  <>
                    This payment never clears the balance — the interest outruns it. Raise it, or
                    check whether the rate is a yearly percentage.
                  </>
                ) : (
                  <>
                    Paid off {monthLabel(monthOf(debt.paidOffOn))}
                    {debt.endsOn !== null && ` · contract runs to ${monthLabel(monthOf(debt.endsOn))}`}
                    {early !== null && <span className="tag">{early}</span>}
                  </>
                )}
              </p>

              {debt.interestRemainingBase > 0 && (
                <p className="notes">
                  {money(debt.interestRemainingBase, baseCurrency)} of interest still to pay from
                  here.
                </p>
              )}

              {debt.balloonBase > 0 && (
                <p className="notes">
                  {money(debt.balloonBase, baseCurrency)} still standing when the contract ends — a
                  residual somebody has to settle.
                </p>
              )}

              {debt.assetValue !== null && (
                <p className="notes">
                  {started ? 'Bought' : 'Buys'} something worth{' '}
                  {money(debt.assetValue, debt.currency)}, held flat in the projection.
                  {!started && ' Neither it nor the debt counts until then.'}
                </p>
              )}

              {(downFrom !== undefined || landsIn !== undefined) && (
                <p className="notes">
                  {downFrom !== undefined &&
                    `${money(debt.downPayment ?? 0, debt.currency)} down out of ${downFrom.name}`}
                  {downFrom !== undefined && landsIn !== undefined && ' · '}
                  {landsIn !== undefined && `borrowing lands in ${landsIn.name}`}
                </p>
              )}

              {debt.payments.length > 0 && (
                <ul>
                  {debt.payments.map((payment) => (
                    <li key={payment.id}>
                      <span className="title">{money(payment.amount, debt.currency)} off the principal</span>
                      <span className="notes">
                        {payment.paidOn}
                        {payment.note !== null && ` · ${payment.note}`}
                      </span>
                      <span className="actions">
                        <button
                          type="button"
                          className="danger"
                          disabled={busyId === payment.id}
                          onClick={() => onDeletePayment(payment.id)}
                        >
                          Remove
                        </button>
                      </span>
                    </li>
                  ))}
                </ul>
              )}

              {payingId === debt.id && (
                <PaymentForm
                  debt={debt}
                  accounts={accounts}
                  onSaved={() => {
                    setPayingId(null);
                    reload();
                  }}
                  onCancel={() => setPayingId(null)}
                  onError={fail}
                />
              )}

              <div className="actions">
                {/* Offered on a debt not taken out yet too: dating a chunk in
                    the future is how you try one on before committing to it. */}
                {!cleared && payingId !== debt.id && (
                  <button type="button" onClick={() => setPayingId(debt.id)}>
                    Pay off a chunk
                  </button>
                )}
                <button
                  type="button"
                  className="danger"
                  disabled={busyId === debt.id}
                  onClick={() => onDelete(debt)}
                >
                  Delete
                </button>
              </div>
            </section>
          );
        })
      )}
    </>
  );
}

function DebtForm({
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
  const [principal, setPrincipal] = useState('');
  const [currency, setCurrency] = useState(baseCurrency);
  const [annualRate, setAnnualRate] = useState('');
  const [payment, setPayment] = useState('');
  const [startsOn, setStartsOn] = useState(today());
  const [endsOn, setEndsOn] = useState('');
  const [assetValue, setAssetValue] = useState('');
  const [downPayment, setDownPayment] = useState('');
  const [downPaymentAccountId, setDownPaymentAccountId] = useState('');
  const [disbursesToAccountId, setDisbursesToAccountId] = useState('');
  const [saving, setSaving] = useState(false);

  // "A set amount until X date" is how anyone thinks about this, but the amount
  // that actually clears a balance by a date is an annuity nobody works out in
  // their head. Type the date, press the button, get the payment.
  const suggestion =
    endsOn === '' || principal === ''
      ? null
      : annuityPayment(Number(principal), Number(annualRate || 0), monthsBetween(startsOn, endsOn));

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);

    try {
      await createDebt({
        name,
        principal: Number(principal),
        currency,
        annualRate: Number(annualRate || 0),
        payment: Number(payment),
        startsOn,
        endsOn: endsOn === '' ? null : endsOn,
        assetValue: assetValue === '' ? null : Number(assetValue),
        downPayment: downPayment === '' ? null : Number(downPayment),
        downPaymentAccountId:
          downPayment === '' || downPaymentAccountId === '' ? null : downPaymentAccountId,
        disbursesToAccountId: disbursesToAccountId === '' ? null : disbursesToAccountId,
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
          Borrowed
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
          {/* As the lender writes it: 3.25 means 3.25%. Zero is a real answer —
              an interest-free instalment plan. */}
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
          First payment
          <input
            type="date"
            value={startsOn}
            onChange={(e) => setStartsOn(e.target.value)}
            required
          />
        </label>

        <label>
          Until
          <input type="date" value={endsOn} onChange={(e) => setEndsOn(e.target.value)} />
        </label>
      </div>

      <div className="row">
        <label>
          Payment a month
          <input
            type="number"
            step="0.01"
            min="0.01"
            value={payment}
            onChange={(e) => setPayment(e.target.value)}
            required
          />
        </label>

        {suggestion !== null && (
          <button type="button" className="ghost" onClick={() => setPayment(suggestion.toFixed(2))}>
            Use {suggestion.toFixed(2)} — clears it by then
          </button>
        )}
      </div>

      <div className="row">
        <label>
          {/* Without this a mortgage reads as a loss the size of itself. */}
          What it buys, worth
          <input
            type="number"
            step="0.01"
            min="0"
            value={assetValue}
            onChange={(e) => setAssetValue(e.target.value)}
          />
        </label>

        <label>
          Downpayment
          <input
            type="number"
            step="0.01"
            min="0"
            value={downPayment}
            onChange={(e) => setDownPayment(e.target.value)}
          />
        </label>

        {downPayment !== '' && (
          <label>
            Out of
            <select
              value={downPaymentAccountId}
              onChange={(e) => setDownPaymentAccountId(e.target.value)}
            >
              <option value="">Already paid — do not touch a balance</option>
              {accounts.map((account) => (
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
          Borrowing lands in
          <select
            value={disbursesToAccountId}
            onChange={(e) => setDisbursesToAccountId(e.target.value)}
          >
            {/* A mortgage: the bank pays the seller and the money never passes
                through an account of yours. */}
            <option value="">Nowhere — it goes straight to the seller</option>
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      <p className="muted">
        Naming accounts here moves the money on the first payment date. Leave them unset for a debt
        you already have — that cash moved long ago and is already in your balances.
      </p>

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

function PaymentForm({
  debt,
  accounts,
  onSaved,
  onCancel,
  onError,
}: {
  debt: Debt;
  accounts: Account[];
  onSaved: () => void;
  onCancel: () => void;
  onError: (e: unknown) => void;
}) {
  const [amount, setAmount] = useState('');

  // A chunk cannot land before the debt exists, and the server says so. For a
  // debt that starts next June, offering today's date would just be an error
  // waiting for the user to press Save.
  const [paidOn, setPaidOn] = useState(debt.startsOn > today() ? debt.startsOn : today());
  const [accountId, setAccountId] = useState('');
  const [note, setNote] = useState('');
  const [saving, setSaving] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);

    try {
      await createDebtPayment(debt.id, {
        amount: Number(amount),
        paidOn,
        accountId: accountId === '' ? null : accountId,
        note: note === '' ? null : note,
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
          Off the principal
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
          On
          {/* Future dates are the point: this is how you try one on before
              you make it. */}
          <input type="date" value={paidOn} onChange={(e) => setPaidOn(e.target.value)} required />
        </label>

        <label>
          Out of
          <select value={accountId} onChange={(e) => setAccountId(e.target.value)}>
            <option value="">Already logged it myself</option>
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="row">
        <label>
          Note
          <input value={note} onChange={(e) => setNote(e.target.value)} maxLength={200} />
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
