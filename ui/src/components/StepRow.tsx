import { useState } from 'react';
import { deleteStep, putStepOnBoard, updateStep } from '../api';
import StepForm from './StepForm';
import { STEP_KIND_LABELS, STEP_STATUS_LABELS, StepStatus } from '../types';
import type { LearningGoal, LearningStep, LearningStepStatus, StepEdit } from '../types';

/** The server takes a whole step, not a patch, so every write starts from this. */
const toEdit = (step: LearningStep): StepEdit => ({
  title: step.title,
  kind: step.kind,
  status: step.status,
  targetOn: step.targetOn,
  notes: step.notes,
  outcome: step.outcome,
  provider: step.provider,
  url: step.url,
  cost: step.cost,
  hours: step.hours,
  sortOrder: step.sortOrder,
});

export default function StepRow({
  step,
  onChanged,
  onReload,
  fail,
}: {
  step: LearningStep;
  onChanged: (goal: LearningGoal) => void;
  onReload: () => Promise<void>;
  fail: (e: unknown) => void;
}) {
  const [busy, setBusy] = useState<string | null>(null);
  const [editing, setEditing] = useState(false);
  const [showOutcome, setShowOutcome] = useState(false);

  const finished = step.status === StepStatus.Done || step.status === StepStatus.Dropped;

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

  const setStatus = (status: LearningStepStatus) =>
    run('status', async () => onChanged(await updateStep(step.id, { ...toEdit(step), status })));

  const save = (edit: StepEdit) =>
    run('save', async () => {
      onChanged(await updateStep(step.id, edit));
      setEditing(false);
    });

  const onBoard = () =>
    run('board', async () => onChanged(await putStepOnBoard(step.id, step.targetOn)));

  const remove = () => {
    if (!window.confirm(`Remove "${step.title}" from the path?`)) {
      return;
    }

    run('delete', async () => {
      await deleteStep(step.id);
      await onReload();
    });
  };

  if (editing) {
    return (
      <li>
        <StepForm step={step} onSave={save} onCancel={() => setEditing(false)} busy={busy === 'save'} />
      </li>
    );
  }

  return (
    <li className={finished ? 'step finished' : 'step'}>
      <div className="step-head">
        <span className="kind">{STEP_KIND_LABELS[step.kind]}</span>

        <span className={step.status === StepStatus.Done ? 'title done' : 'title'}>
          {step.title}
          {step.provider && <small className="notes">{step.provider}</small>}
        </span>

        <span className="due">
          {step.targetOn ?? ''}
          {step.status !== StepStatus.Planned && (
            <small className="notes">{STEP_STATUS_LABELS[step.status]}</small>
          )}
        </span>
      </div>

      {step.notes !== '' && <p className="notes-body">{step.notes}</p>}

      {/* The peek: a step with an outcome says so on the row, and opens on a
          press. That is what makes an assignment's grade findable later
          without opening the form. */}
      {step.outcome !== '' && (
        <button
          type="button"
          className="link outcome-peek"
          onClick={() => setShowOutcome((open) => !open)}
        >
          {showOutcome ? '▾ ' : '▸ '}
          {showOutcome ? 'Result' : step.outcome.split('\n')[0].slice(0, 90)}
        </button>
      )}

      {showOutcome && <p className="outcome">{step.outcome}</p>}

      <div className="actions">
        {step.task ? (
          <span className="muted">On the board for {step.task.dueOn}</span>
        ) : (
          !finished && (
            <button type="button" className="ghost" onClick={onBoard} disabled={busy !== null}>
              Put on board
            </button>
          )
        )}

        {step.status !== StepStatus.Done && (
          <button
            type="button"
            className="ghost"
            onClick={() => setStatus(StepStatus.Done)}
            disabled={busy !== null}
          >
            Mark done
          </button>
        )}

        <button type="button" className="ghost" onClick={() => setEditing(true)} disabled={busy !== null}>
          Edit
        </button>

        <button type="button" className="danger" onClick={remove} disabled={busy !== null}>
          Remove
        </button>
      </div>
    </li>
  );
}
