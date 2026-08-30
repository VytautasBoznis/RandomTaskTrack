import { useState } from 'react';
import { createGoal, updateGoal } from '../api';
import { GOAL_STATUS_LABELS, TIER_LABELS } from '../types';
import type { LearningGoal, LearningGoalStatus, LearningTier } from '../types';

const TIERS: LearningTier[] = [1, 2, 3, 4];
const STATUSES: LearningGoalStatus[] = [1, 2, 3];

const blank = (value: string) => (value.trim() === '' ? null : value.trim());

export default function GoalForm({
  goal,
  onSaved,
  onCancel,
}: {
  goal: LearningGoal | null;
  onSaved: () => void;
  onCancel: () => void;
}) {
  const [title, setTitle] = useState(goal?.title ?? '');
  const [tier, setTier] = useState<LearningTier>(goal?.tier ?? 4);
  const [status, setStatus] = useState<LearningGoalStatus>(goal?.status ?? 1);
  const [why, setWhy] = useState(goal?.why ?? '');
  const [benefits, setBenefits] = useState(goal?.benefits ?? '');
  const [targetOn, setTargetOn] = useState(goal?.targetOn ?? '');
  const [context, setContext] = useState(goal?.context ?? '');
  const [notes, setNotes] = useState(goal?.notes ?? '');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    const draft = {
      title: title.trim(),
      tier,
      why: blank(why),
      benefits: blank(benefits),
      targetOn: blank(targetOn),
      context: blank(context),
      notes: blank(notes),
    };

    try {
      if (goal) {
        await updateGoal(goal.id, { ...draft, status });
      } else {
        await createGoal(draft);
      }

      onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not save');
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="task-form" onSubmit={submit}>
      <label>
        What are you going for?
        <input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Architect promotion"
          required
          maxLength={200}
        />
      </label>

      <div className="row">
        <label>
          Priority
          <select value={tier} onChange={(e) => setTier(Number(e.target.value) as LearningTier)}>
            {TIERS.map((value) => (
              <option key={value} value={value}>
                {TIER_LABELS[value]}
              </option>
            ))}
          </select>
        </label>

        <label>
          Prepared by
          <input type="date" value={targetOn} onChange={(e) => setTargetOn(e.target.value)} />
        </label>

        {/* Only on an existing path: a new one is active by definition. */}
        {goal && (
          <label>
            Status
            <select
              value={status}
              onChange={(e) => setStatus(Number(e.target.value) as LearningGoalStatus)}
            >
              {STATUSES.map((value) => (
                <option key={value} value={value}>
                  {GOAL_STATUS_LABELS[value]}
                </option>
              ))}
            </select>
          </label>
        )}
      </div>

      {/* The two fields the card leads with. They are the reason this tab gets
          opened on a morning when none of it feels worth it. */}
      <label>
        Why you want it
        <textarea
          value={why}
          onChange={(e) => setWhy(e.target.value)}
          rows={2}
          maxLength={2000}
          placeholder="The architect title needs three certs I do not have yet."
        />
      </label>

      <label>
        What you expect out of it
        <textarea
          value={benefits}
          onChange={(e) => setBenefits(e.target.value)}
          rows={2}
          maxLength={2000}
          placeholder="The promotion, the band it comes with, and work I would rather be doing."
        />
      </label>

      <label>
        Where you are starting from
        <textarea
          value={context}
          onChange={(e) => setContext(e.target.value)}
          rows={3}
          maxLength={4000}
          placeholder="Eight years on .NET, day-to-day Azure but no cert, no AWS. About 6 hours a week."
        />
        <small className="muted">
          This is what the path gets drafted from — hours a week, what you already know, anything
          that constrains it. The more honest it is, the less the plan has to guess.
        </small>
      </label>

      <label>
        Notes
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={2} maxLength={4000} />
      </label>

      {error && <p className="error">{error}</p>}

      <div className="actions">
        <button type="submit" disabled={busy || title.trim() === ''}>
          {busy ? 'Saving…' : goal ? 'Save' : 'Add path'}
        </button>
        <button type="button" className="ghost" onClick={onCancel} disabled={busy}>
          Cancel
        </button>
      </div>
    </form>
  );
}
