import { useState } from 'react';
import { STEP_KIND_LABELS, STEP_STATUS_LABELS, StepKinds } from '../types';
import type {
  LearningStep,
  LearningStepKind,
  LearningStepStatus,
  StepEdit,
  StepInput,
} from '../types';

const KINDS: LearningStepKind[] = [1, 2, 3, 4, 5, 6, 7];
const STATUSES: LearningStepStatus[] = [1, 2, 3, 4];

const blank = (value: string) => (value.trim() === '' ? null : value.trim());

/**
 * One form for both jobs. Adding a step takes the plan half; editing one adds
 * the status and the outcome, which only exist once there is something to
 * report. `onSave` is typed to whichever the caller wants back.
 */
export default function StepForm({
  step,
  onSave,
  onCancel,
  busy,
}: {
  step: LearningStep | null;
  onSave: (edit: StepEdit & StepInput) => void;
  onCancel: () => void;
  busy: boolean;
}) {
  const [title, setTitle] = useState(step?.title ?? '');
  const [kind, setKind] = useState<LearningStepKind>(step?.kind ?? StepKinds.Study);
  const [status, setStatus] = useState<LearningStepStatus>(step?.status ?? 1);
  const [targetOn, setTargetOn] = useState(step?.targetOn ?? '');
  const [notes, setNotes] = useState(step?.notes ?? '');
  const [outcome, setOutcome] = useState(step?.outcome ?? '');
  const [provider, setProvider] = useState(step?.provider ?? '');
  const [url, setUrl] = useState(step?.url ?? '');
  const [cost, setCost] = useState(step?.cost ?? '');
  const [hours, setHours] = useState(step?.hours?.toString() ?? '');

  function submit(event: React.FormEvent) {
    event.preventDefault();

    onSave({
      title: title.trim(),
      kind,
      status,
      targetOn: blank(targetOn),
      notes: blank(notes),
      outcome: blank(outcome),
      provider: blank(provider),
      url: blank(url),
      cost: blank(cost),
      hours: hours.trim() === '' ? null : Number(hours),
      sortOrder: step?.sortOrder ?? 0,
    });
  }

  return (
    <form className="task-form" onSubmit={submit}>
      <label>
        Step
        <input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Sit AZ-305"
          required
          maxLength={300}
        />
      </label>

      <div className="row">
        <label>
          Kind
          <select value={kind} onChange={(e) => setKind(Number(e.target.value) as LearningStepKind)}>
            {KINDS.map((value) => (
              <option key={value} value={value}>
                {STEP_KIND_LABELS[value]}
              </option>
            ))}
          </select>
        </label>

        <label>
          {kind === StepKinds.Assignment ? 'Due' : 'Target date'}
          <input type="date" value={targetOn} onChange={(e) => setTargetOn(e.target.value)} />
        </label>

        {/* Status and outcome only exist once the step does. On a new one there
            is nothing to report yet. */}
        {step && (
          <label>
            Status
            <select
              value={status}
              onChange={(e) => setStatus(Number(e.target.value) as LearningStepStatus)}
            >
              {STATUSES.map((value) => (
                <option key={value} value={value}>
                  {STEP_STATUS_LABELS[value]}
                </option>
              ))}
            </select>
          </label>
        )}
      </div>

      <label>
        What to do
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={2} maxLength={4000} />
      </label>

      {step && (
        <label>
          Result
          <textarea
            value={outcome}
            onChange={(e) => setOutcome(e.target.value)}
            rows={3}
            maxLength={4000}
            placeholder="8.0. Lost marks on the derivation in Q3. Retake not needed."
          />
          <small className="muted">
            The grade, the mark breakdown, or what went wrong and when the retake is. Kept apart
            from the plan above, and flagged on the row so it is findable later.
          </small>
        </label>
      )}

      <div className="row">
        <label>
          Provider
          <input
            value={provider}
            onChange={(e) => setProvider(e.target.value)}
            placeholder="Udemy"
            maxLength={200}
          />
        </label>

        <label>
          Cost
          <input value={cost} onChange={(e) => setCost(e.target.value)} placeholder="€14.99" maxLength={100} />
        </label>

        <label>
          Hours
          <input
            type="number"
            min={1}
            max={1000}
            value={hours}
            onChange={(e) => setHours(e.target.value)}
          />
        </label>
      </div>

      <label>
        Link
        <input value={url} onChange={(e) => setUrl(e.target.value)} maxLength={1000} />
      </label>

      <div className="actions">
        <button type="submit" disabled={busy || title.trim() === ''}>
          {busy ? 'Saving…' : step ? 'Save' : 'Add step'}
        </button>
        <button type="button" className="ghost" onClick={onCancel} disabled={busy}>
          Cancel
        </button>
      </div>
    </form>
  );
}
