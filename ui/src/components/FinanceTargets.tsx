import { useCallback, useEffect, useState } from 'react';
import { createTarget, deleteTarget, getFinanceOverview } from '../api';
import { useApiError } from '../hooks';
import type { FinanceTarget } from '../types';

/**
 * Marks on the projection. Both fields are optional but not both at once: an
 * amount alone is a goal line, a date alone is a milestone, both together is a
 * point to hit.
 */
export default function FinanceTargets({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [targets, setTargets] = useState<FinanceTarget[] | null>(null);
  const [baseCurrency, setBaseCurrency] = useState('EUR');
  const [adding, setAdding] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);

  const reload = useCallback(
    () =>
      getFinanceOverview()
        .then((o) => {
          setTargets(o.targets);
          setBaseCurrency(o.baseCurrency);
        })
        .catch(fail),
    [fail],
  );

  useEffect(() => {
    reload();
  }, [reload]);

  async function onDelete(target: FinanceTarget) {
    setBusyId(target.id);
    setError(null);

    try {
      await deleteTarget(target.id);
      await reload();
    } catch (e) {
      fail(e);
    } finally {
      setBusyId(null);
    }
  }

  if (!targets) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  return (
    <>
      <div className="toolbar">
        <p className="today">Drawn on the net worth chart.</p>
        <button type="button" onClick={() => setAdding(true)}>
          Add target
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {adding && (
        <TargetForm
          baseCurrency={baseCurrency}
          onSaved={() => {
            setAdding(false);
            reload();
          }}
          onCancel={() => setAdding(false)}
          onError={fail}
        />
      )}

      {targets.length === 0 ? (
        <p className="empty">No targets yet.</p>
      ) : (
        <section className="card">
          <ul>
            {targets.map((target) => (
              <li key={target.id}>
                <span className="title">{target.label}</span>
                <span className="notes">
                  {target.amount !== null && `${target.amount.toLocaleString()} ${baseCurrency}`}
                  {target.amount !== null && target.targetOn !== null && ' by '}
                  {target.targetOn}
                </span>
                <span className="actions">
                  <button
                    type="button"
                    className="danger"
                    disabled={busyId === target.id}
                    onClick={() => onDelete(target)}
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

function TargetForm({
  baseCurrency,
  onSaved,
  onCancel,
  onError,
}: {
  baseCurrency: string;
  onSaved: () => void;
  onCancel: () => void;
  onError: (e: unknown) => void;
}) {
  const [label, setLabel] = useState('');
  const [amount, setAmount] = useState('');
  const [targetOn, setTargetOn] = useState('');
  const [saving, setSaving] = useState(false);

  const nothingToDraw = amount === '' && targetOn === '';

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);

    try {
      await createTarget({
        label,
        targetOn: targetOn === '' ? null : targetOn,
        amount: amount === '' ? null : Number(amount),
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
          Label
          <input value={label} onChange={(e) => setLabel(e.target.value)} required maxLength={200} />
        </label>

        <label>
          Amount, {baseCurrency}
          <input
            type="number"
            step="1"
            min="0"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            placeholder="a goal line"
          />
        </label>

        <label>
          By
          <input type="date" value={targetOn} onChange={(e) => setTargetOn(e.target.value)} />
        </label>
      </div>

      {nothingToDraw && <p className="muted">Set an amount, a date, or both — otherwise there is nothing to draw.</p>}

      <div className="actions">
        <button type="submit" disabled={saving || nothingToDraw}>
          {saving ? 'Saving…' : 'Save'}
        </button>
        <button type="button" className="ghost" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}
