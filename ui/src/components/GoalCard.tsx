import { useState } from 'react';
import { addSteps, deleteGoal, draftPlan } from '../api';
import PlanPanel from './PlanPanel';
import StepRow from './StepRow';
import StepForm from './StepForm';
import { StepStatus, TIER_LABELS } from '../types';
import type { LearningGoal, StepInput } from '../types';

/** "in 47 days" / "8 days ago" — the same phrasing the credentials pane uses. */
function countdown(days: number): string {
  if (days === 0) return 'today';
  if (days < 0) return `${-days} day${days === -1 ? '' : 's'} ago`;

  if (days < 60) return `in ${days} days`;

  const months = Math.round(days / 30);

  return months < 24 ? `in ${months} months` : `in ${Math.round(days / 365)} years`;
}

export default function GoalCard({
  goal,
  onChanged,
  onReload,
  onEdit,
  fail,
}: {
  goal: LearningGoal;
  /** The write returned the whole goal, so the card can swap itself out. */
  onChanged: (goal: LearningGoal) => void;
  /** For writes whose blast radius is wider than this card. */
  onReload: () => Promise<void>;
  onEdit: () => void;
  fail: (e: unknown) => void;
}) {
  const [busy, setBusy] = useState<string | null>(null);
  const [asking, setAsking] = useState(false);
  const [context, setContext] = useState(goal.context);
  const [adding, setAdding] = useState(false);

  // Dropped steps are decisions, not work: they stay visible but they are not
  // part of what "done" is measured against.
  const live = goal.steps.filter((step) => step.status !== StepStatus.Dropped);
  const done = live.filter((step) => step.status === StepStatus.Done).length;

  async function run(what: string, action: () => Promise<unknown>) {
    setBusy(what);

    try {
      await action();
    } catch (e) {
      fail(e);
    } finally {
      setBusy(null);
    }
  }

  const draft = () =>
    run('draft', async () => {
      onChanged(await draftPlan(goal.id, context.trim() === '' ? null : context.trim()));
      setAsking(false);
    });

  const commit = (steps: StepInput[]) =>
    run('steps', async () => {
      onChanged(await addSteps(goal.id, steps));
      setAdding(false);
    });

  const remove = () => {
    if (!window.confirm(`Delete "${goal.title}", its steps and anything they have on the board?`)) {
      return;
    }

    run('delete', async () => {
      await deleteGoal(goal.id);
      await onReload();
    });
  };

  return (
    <section className="card goal">
      <div className="goal-head">
        <h2>
          {goal.title}
          <span className="kind">{TIER_LABELS[goal.tier]}</span>
        </h2>

        <p className="muted">
          {goal.targetOn && <>Prepared by {goal.targetOn} — {countdown(goal.daysUntilTarget ?? 0)}. </>}
          {live.length > 0 && `${done} of ${live.length} steps done.`}
        </p>
      </div>

      {/* The motivation leads, before any of the work does. It is the reason
          this tab gets opened on a morning when none of it feels worth it. */}
      {goal.why !== '' && <p className="why">{goal.why}</p>}
      {goal.benefits !== '' && <p className="benefits">{goal.benefits}</p>}
      {goal.notes !== '' && <p className="muted">{goal.notes}</p>}

      <div className="actions">
        <button type="button" onClick={() => setAsking(true)} disabled={busy !== null}>
          {busy === 'draft' ? 'Researching…' : goal.plan ? 'Draft it again' : 'Draft the path'}
        </button>
        <button type="button" className="ghost" onClick={() => setAdding(true)} disabled={busy !== null}>
          Add step
        </button>
        <button type="button" className="ghost" onClick={onEdit} disabled={busy !== null}>
          Edit
        </button>
        <button type="button" className="danger" onClick={remove} disabled={busy !== null}>
          Delete
        </button>
      </div>

      {asking && (
        <div className="ask">
          <label>
            Where you are starting from
            <textarea value={context} onChange={(e) => setContext(e.target.value)} rows={3} maxLength={4000} />
          </label>

          <p className="muted">
            Drafting again replaces the suggestion below. The steps you have already committed to
            are left alone.
          </p>

          <div className="actions">
            <button type="button" onClick={draft} disabled={busy !== null}>
              {busy === 'draft' ? 'Researching…' : 'Draft it'}
            </button>
            <button
              type="button"
              className="ghost"
              onClick={() => {
                setAsking(false);
                setContext(goal.context);
              }}
              disabled={busy !== null}
            >
              Cancel
            </button>
          </div>
        </div>
      )}

      {adding && (
        <StepForm
          step={null}
          onSave={(input) => commit([input])}
          onCancel={() => setAdding(false)}
          busy={busy === 'steps'}
        />
      )}

      <h3>Path</h3>

      {goal.steps.length === 0 ? (
        <p className="empty">
          Nothing committed to yet. Draft the path, then add the lines you actually mean to do.
        </p>
      ) : (
        <ul className="steps-list">
          {goal.steps.map((step) => (
            <StepRow key={step.id} step={step} onChanged={onChanged} onReload={onReload} fail={fail} />
          ))}
        </ul>
      )}

      {goal.plan && (
        <PlanPanel
          plan={goal.plan}
          researchedAt={goal.researchedAt}
          taken={new Set(goal.steps.map((step) => step.title.toLowerCase()))}
          onAdd={commit}
          busy={busy === 'steps'}
        />
      )}
    </section>
  );
}
