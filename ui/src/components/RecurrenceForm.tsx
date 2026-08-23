import { useState, type FormEvent } from 'react';
import { createRecurrence, updateRecurrence } from '../api';
import { AnchorMode, RuleType } from '../types';
import type { Recurrence, RecurrenceAnchorMode, RecurrenceRuleType, TaskDomain } from '../types';

/** Indexed by the server's day numbering: 0 = Sunday … 6 = Saturday. */
export const WEEKDAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

export default function RecurrenceForm({
  domains,
  recurrence,
  onSaved,
  onCancel,
}: {
  domains: TaskDomain[];
  recurrence: Recurrence | null;
  onSaved: () => void;
  onCancel: () => void;
}) {
  const [domainId, setDomainId] = useState(recurrence?.domainId ?? domains[0]?.id ?? 0);
  const [title, setTitle] = useState(recurrence?.title ?? '');
  const [notes, setNotes] = useState(recurrence?.notes ?? '');
  const [ruleType, setRuleType] = useState<RecurrenceRuleType>(recurrence?.ruleType ?? RuleType.IntervalDays);
  const [intervalDays, setIntervalDays] = useState(String(recurrence?.intervalDays ?? 7));
  const [daysOfWeek, setDaysOfWeek] = useState<number[]>(recurrence?.daysOfWeek ?? []);
  const [dayOfMonth, setDayOfMonth] = useState(String(recurrence?.dayOfMonth ?? 1));
  const [anchorMode, setAnchorMode] = useState<RecurrenceAnchorMode>(recurrence?.anchorMode ?? AnchorMode.FromSchedule);
  const [timeOfDay, setTimeOfDay] = useState(recurrence?.timeOfDay?.slice(0, 5) ?? '');
  const [startsOn, setStartsOn] = useState(recurrence?.startsOn ?? '');
  const [endsOn, setEndsOn] = useState(recurrence?.endsOn ?? '');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const toggleDay = (day: number) =>
    setDaysOfWeek((current) => (current.includes(day) ? current.filter((d) => d !== day) : [...current, day].sort()));

  async function submit(event: FormEvent) {
    event.preventDefault();

    if (ruleType === RuleType.DaysOfWeek && daysOfWeek.length === 0) {
      setError('Pick at least one weekday');
      return;
    }

    setBusy(true);
    setError(null);

    const draft = {
      domainId,
      title,
      // Update reads null as "leave alone", so clearing notes has to send "".
      notes: notes.trim() === '' && recurrence === null ? null : notes.trim(),
      ruleType,
      // Only the rule in force is sent; the schema's shape constraint is an OR,
      // so the columns left over from another rule type do no harm.
      intervalDays: ruleType === RuleType.IntervalDays ? Number(intervalDays) : null,
      daysOfWeek: ruleType === RuleType.DaysOfWeek ? daysOfWeek : null,
      dayOfMonth: ruleType === RuleType.DayOfMonth ? Number(dayOfMonth) : null,
      anchorMode,
      timeOfDay: timeOfDay === '' ? null : `${timeOfDay}:00`,
      startsOn: startsOn === '' ? null : startsOn,
      endsOn: endsOn === '' ? null : endsOn,
    };

    try {
      await (recurrence ? updateRecurrence(recurrence.id, draft) : createRecurrence(draft));
      onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not save the recurrence');
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card task-form" onSubmit={submit}>
      <h2>{recurrence ? 'Edit recurrence' : 'New recurrence'}</h2>

      <label>
        Domain
        <select
          value={domainId}
          disabled={recurrence !== null}
          onChange={(e) => setDomainId(Number(e.target.value))}
        >
          {domains.map((domain) => (
            <option key={domain.id} value={domain.id}>
              {domain.name}
            </option>
          ))}
        </select>
      </label>

      <label>
        Title
        <input value={title} onChange={(e) => setTitle(e.target.value)} required maxLength={500} autoFocus />
      </label>

      <label>
        Notes
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={2} />
      </label>

      <label>
        Repeats
        <select value={ruleType} onChange={(e) => setRuleType(Number(e.target.value) as RecurrenceRuleType)}>
          <option value={RuleType.IntervalDays}>Every N days</option>
          <option value={RuleType.DaysOfWeek}>On weekdays</option>
          <option value={RuleType.DayOfMonth}>Monthly on a date</option>
        </select>
      </label>

      {ruleType === RuleType.IntervalDays && (
        <label>
          Every (days)
          <input
            type="number"
            min={1}
            value={intervalDays}
            onChange={(e) => setIntervalDays(e.target.value)}
            required
          />
        </label>
      )}

      {ruleType === RuleType.DaysOfWeek && (
        <fieldset className="weekdays">
          <legend>Weekdays</legend>
          {WEEKDAYS.map((label, day) => (
            <label key={label} className="checkbox">
              <input type="checkbox" checked={daysOfWeek.includes(day)} onChange={() => toggleDay(day)} />
              {label}
            </label>
          ))}
        </fieldset>
      )}

      {ruleType === RuleType.DayOfMonth && (
        <label>
          Day of month
          <input
            type="number"
            min={1}
            max={31}
            value={dayOfMonth}
            onChange={(e) => setDayOfMonth(e.target.value)}
            required
          />
        </label>
      )}

      <label>
        Next one is measured
        <select value={anchorMode} onChange={(e) => setAnchorMode(Number(e.target.value) as RecurrenceAnchorMode)}>
          <option value={AnchorMode.FromSchedule}>From the schedule — a fixed cadence</option>
          <option value={AnchorMode.FromCompletion}>From completion — restart the clock when it's done</option>
        </select>
      </label>

      <div className="row">
        <label>
          Time (optional)
          <input type="time" value={timeOfDay} onChange={(e) => setTimeOfDay(e.target.value)} />
        </label>

        <label>
          Starts on
          <input
            type="date"
            value={startsOn}
            disabled={recurrence !== null}
            onChange={(e) => setStartsOn(e.target.value)}
          />
        </label>

        <label>
          Ends on (optional)
          <input type="date" value={endsOn} onChange={(e) => setEndsOn(e.target.value)} />
        </label>
      </div>

      {error && <p className="error">{error}</p>}

      <div className="actions">
        <button type="submit" disabled={busy || domainId === 0}>
          {busy ? 'Saving…' : 'Save'}
        </button>
        <button type="button" className="link" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}
