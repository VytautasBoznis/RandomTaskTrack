import { useState, type FormEvent } from 'react';
import { createTask, updateTask } from '../api';
import type { TaskDomain, TaskListItem } from '../types';

export default function TaskForm({
  domains,
  task,
  today,
  onSaved,
  onCancel,
}: {
  domains: TaskDomain[];
  task: TaskListItem | null;
  today: string;
  onSaved: () => void;
  onCancel: () => void;
}) {
  const [domainId, setDomainId] = useState(task?.domainId ?? domains[0]?.id ?? 0);
  const [title, setTitle] = useState(task?.title ?? '');
  const [notes, setNotes] = useState(task?.notes ?? '');
  const [dueOn, setDueOn] = useState(task?.dueOn ?? today);
  // <input type="time"> speaks "HH:mm"; TimeOnly wants "HH:mm:ss".
  const [dueTime, setDueTime] = useState(task?.dueTime?.slice(0, 5) ?? '');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    const draft = {
      domainId,
      title,
      // Update reads null as "leave alone", so clearing notes has to send "".
      notes: notes.trim() === '' && task === null ? null : notes.trim(),
      dueOn,
      dueTime: dueTime === '' ? null : `${dueTime}:00`,
    };

    try {
      await (task ? updateTask(task.id, draft) : createTask(draft));
      onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not save the task');
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card task-form" onSubmit={submit}>
      <h2>{task ? 'Edit task' : 'New task'}</h2>

      <label>
        Domain
        <select value={domainId} onChange={(e) => setDomainId(Number(e.target.value))}>
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

      <div className="row">
        <label>
          Due date
          <input type="date" value={dueOn} onChange={(e) => setDueOn(e.target.value)} required />
        </label>

        <label>
          Time (optional)
          <input type="time" value={dueTime} onChange={(e) => setDueTime(e.target.value)} />
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
