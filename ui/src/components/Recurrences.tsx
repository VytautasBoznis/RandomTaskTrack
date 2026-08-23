import { useCallback, useEffect, useState } from 'react';
import { deleteRecurrence, getRecurrences, setRecurrenceActive } from '../api';
import { useApiError, useDomains } from '../hooks';
import RecurrenceForm, { WEEKDAYS } from './RecurrenceForm';
import { AnchorMode, RuleType } from '../types';
import type { Recurrence } from '../types';

function describeRule(recurrence: Recurrence) {
  const cadence =
    recurrence.ruleType === RuleType.IntervalDays
      ? `Every ${recurrence.intervalDays} day${recurrence.intervalDays === 1 ? '' : 's'}`
      : recurrence.ruleType === RuleType.DaysOfWeek
        ? `Weekly on ${(recurrence.daysOfWeek ?? []).map((day) => WEEKDAYS[day]).join(', ')}`
        : `Monthly on day ${recurrence.dayOfMonth}`;

  const anchor = recurrence.anchorMode === AnchorMode.FromCompletion ? ', from completion' : '';
  const time = recurrence.timeOfDay ? ` at ${recurrence.timeOfDay.slice(0, 5)}` : '';
  const until = recurrence.endsOn ? ` until ${recurrence.endsOn}` : '';

  return `${cadence}${anchor}${time}${until}`;
}

export default function Recurrences({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const domains = useDomains(fail);
  const [recurrences, setRecurrences] = useState<Recurrence[] | null>(null);
  const [editing, setEditing] = useState<Recurrence | 'new' | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const reload = useCallback(() => getRecurrences().then(setRecurrences).catch(fail), [fail]);

  useEffect(() => {
    reload();
  }, [reload]);

  async function run(id: string, action: () => Promise<unknown>) {
    setBusyId(id);
    setError(null);

    try {
      await action();
      await reload();
    } catch (e) {
      fail(e);
    } finally {
      setBusyId(null);
    }
  }

  const onDelete = (recurrence: Recurrence) => {
    if (window.confirm(`Delete "${recurrence.title}" and its pending tasks? Completed history is kept.`)) {
      run(recurrence.id, () => deleteRecurrence(recurrence.id));
    }
  };

  if (!recurrences) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  return (
    <>
      <div className="toolbar">
        <p className="today">Changing a schedule rebuilds pending tasks. Completed history is never touched.</p>
        <button type="button" onClick={() => setEditing('new')}>
          New recurrence
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {editing && (
        <RecurrenceForm
          key={editing === 'new' ? 'new' : editing.id}
          domains={domains}
          recurrence={editing === 'new' ? null : editing}
          onSaved={() => {
            setEditing(null);
            reload();
          }}
          onCancel={() => setEditing(null)}
        />
      )}

      <section className="card">
        <h2>
          Recurrences <span className="count">{recurrences.length}</span>
        </h2>

        {recurrences.length === 0 ? (
          <p className="empty">Nothing repeats yet.</p>
        ) : (
          <ul>
            {recurrences.map((recurrence) => (
              <li key={recurrence.id} className={recurrence.isActive ? undefined : 'paused'}>
                <span className="domain">{recurrence.domainCode}</span>
                <span className="title">
                  {recurrence.title}
                  <small className="notes">{describeRule(recurrence)}</small>
                </span>
                {!recurrence.isActive && <span className="due">Paused</span>}

                <span className="actions">
                  <button
                    type="button"
                    className="ghost"
                    disabled={busyId === recurrence.id}
                    onClick={() => run(recurrence.id, () => setRecurrenceActive(recurrence.id, !recurrence.isActive))}
                  >
                    {recurrence.isActive ? 'Pause' : 'Resume'}
                  </button>
                  <button type="button" className="link" onClick={() => setEditing(recurrence)}>
                    Edit
                  </button>
                  <button
                    type="button"
                    className="link"
                    disabled={busyId === recurrence.id}
                    onClick={() => onDelete(recurrence)}
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
