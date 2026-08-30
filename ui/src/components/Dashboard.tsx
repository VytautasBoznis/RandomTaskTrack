import { useCallback, useEffect, useState } from 'react';
import { completeTask, deleteTask, getDashboard } from '../api';
import { useApiError, useDomains } from '../hooks';
import TaskForm from './TaskForm';
import { TaskStatus } from '../types';
import type { DashboardLearning, Dashboard as DashboardData, TaskItemStatus, TaskListItem } from '../types';

const COLLAPSED_COUNT = 3;

/**
 * What the learning tab has on the board, one row per path. Deliberately not a
 * Bucket: there is nothing to do to these here. A step is finished on its path,
 * where the outcome and the next step are, so every row is a way through to the
 * Learning tab and nothing else.
 */
function LearningSection({ rows, today, onOpen }: { rows: DashboardLearning[]; today: string; onOpen: () => void }) {
  if (rows.length === 0) {
    return null;
  }

  return (
    <section className="card learning-board">
      <h2>
        Learning <span className="count">{rows.length}</span>
      </h2>

      <ul>
        {rows.map((row) => (
          <li key={row.goalId ?? row.title}>
            {/* The whole row is the button. A text-sized target is not enough
                on the tablet this is read on. */}
            <button type="button" className="row-link" onClick={onOpen}>
              <span className="title">{row.title}</span>
              <span className={row.nextDueOn < today ? 'due overdue' : 'due'}>
                {row.count > 1 && `${row.count} · `}
                {row.nextDueOn}
              </span>
              <span className="go">›</span>
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}

function Bucket({
  title,
  tasks,
  busyId,
  onComplete,
  onEdit,
  onDelete,
}: {
  title: string;
  tasks: TaskListItem[];
  busyId: string | null;
  onComplete: (task: TaskListItem, status: TaskItemStatus) => void;
  onEdit: (task: TaskListItem) => void;
  onDelete: (task: TaskListItem) => void;
}) {
  const [expanded, setExpanded] = useState(false);
  const shown = expanded ? tasks : tasks.slice(0, COLLAPSED_COUNT);

  return (
    <section className="card">
      <h2>
        {title} <span className="count">{tasks.length}</span>
      </h2>

      {tasks.length === 0 ? (
        <p className="empty">Nothing here.</p>
      ) : (
        <ul>
          {shown.map((task) => (
            <li key={task.id}>
              <span className="domain">{task.domainName}</span>
              <span className="title">
                {task.title}
                {task.notes && <small className="notes">{task.notes}</small>}
              </span>
              <span className="due">{task.dueTime ? `${task.dueOn} ${task.dueTime.slice(0, 5)}` : task.dueOn}</span>

              <span className="actions">
                {task.status === TaskStatus.Pending && (
                  <>
                    <button type="button" disabled={busyId === task.id} onClick={() => onComplete(task, TaskStatus.Done)}>
                      Done
                    </button>
                    <button
                      type="button"
                      className="ghost"
                      disabled={busyId === task.id}
                      onClick={() => onComplete(task, TaskStatus.Skipped)}
                    >
                      Skip
                    </button>
                    <button type="button" className="link" onClick={() => onEdit(task)}>
                      Edit
                    </button>
                  </>
                )}
                <button type="button" className="link" disabled={busyId === task.id} onClick={() => onDelete(task)}>
                  Delete
                </button>
              </span>
            </li>
          ))}
        </ul>
      )}

      {tasks.length > COLLAPSED_COUNT && (
        <button type="button" className="link more" onClick={() => setExpanded(!expanded)}>
          {expanded ? 'Show less' : `Show ${tasks.length - COLLAPSED_COUNT} more`}
        </button>
      )}
    </section>
  );
}

export default function Dashboard({
  onUnauthorized,
  onOpenLearning,
}: {
  onUnauthorized: () => void;
  onOpenLearning: () => void;
}) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const domains = useDomains(fail);
  const [data, setData] = useState<DashboardData | null>(null);
  const [editing, setEditing] = useState<TaskListItem | 'new' | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  // Every mutation re-reads the dashboard: completing can spawn the next
  // occurrence of a recurrence and always moves the streak counts, so
  // patching the buckets in place would only be a second, wronger, copy of
  // the server's bucketing.
  const reload = useCallback(() => getDashboard().then(setData).catch(fail), [fail]);

  useEffect(() => {
    reload();
  }, [reload]);

  async function run(task: TaskListItem, action: () => Promise<unknown>) {
    setBusyId(task.id);
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

  const onComplete = (task: TaskListItem, status: TaskItemStatus) => run(task, () => completeTask(task.id, status));

  const onDelete = (task: TaskListItem) => {
    if (window.confirm(`Delete "${task.title}"?`)) {
      run(task, () => deleteTask(task.id));
    }
  };

  if (!data) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  return (
    <>
      <div className="toolbar">
        <p className="today">{data.today}</p>
        <button type="button" onClick={() => setEditing('new')}>
          Add task
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {editing && (
        <TaskForm
          key={editing === 'new' ? 'new' : editing.id}
          domains={domains}
          task={editing === 'new' ? null : editing}
          today={data.today}
          onSaved={() => {
            setEditing(null);
            reload();
          }}
          onCancel={() => setEditing(null)}
        />
      )}

      <div className="buckets">
        <Bucket title="Overdue" tasks={data.overdue} busyId={busyId} onComplete={onComplete} onEdit={setEditing} onDelete={onDelete} />
        <Bucket title="Today" tasks={data.dueToday} busyId={busyId} onComplete={onComplete} onEdit={setEditing} onDelete={onDelete} />
        <Bucket title="Upcoming" tasks={data.upcoming} busyId={busyId} onComplete={onComplete} onEdit={setEditing} onDelete={onDelete} />
        <Bucket
          title="Done today"
          tasks={data.completedToday}
          busyId={busyId}
          onComplete={onComplete}
          onEdit={setEditing}
          onDelete={onDelete}
        />
      </div>

      <LearningSection rows={data.learning} today={data.today} onOpen={onOpenLearning} />

      <section className="card">
        <h2>Last 7 days</h2>
        <ul className="streaks">
          {data.streaks.map((streak) => (
            <li key={streak.domainId}>
              <span className="domain">{streak.domainName}</span>
              <span>
                {streak.completedLast7Days} done · {streak.skippedLast7Days} skipped · {streak.pendingOverdue} overdue
              </span>
            </li>
          ))}
        </ul>
      </section>
    </>
  );
}
