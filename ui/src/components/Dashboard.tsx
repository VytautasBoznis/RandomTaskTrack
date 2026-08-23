import { useEffect, useState } from 'react';
import { ApiError, getDashboard } from '../api';
import type { Dashboard as DashboardData, TaskListItem } from '../types';

function Bucket({ title, tasks }: { title: string; tasks: TaskListItem[] }) {
  return (
    <section className="card">
      <h2>
        {title} <span className="count">{tasks.length}</span>
      </h2>

      {tasks.length === 0 ? (
        <p className="empty">Nothing here.</p>
      ) : (
        <ul>
          {tasks.map((task) => (
            <li key={task.id}>
              <span className="domain">{task.domainName}</span>
              <span className="title">{task.title}</span>
              <span className="due">{task.dueTime ? `${task.dueOn} ${task.dueTime.slice(0, 5)}` : task.dueOn}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

export default function Dashboard({ onUnauthorized }: { onUnauthorized: () => void }) {
  const [data, setData] = useState<DashboardData | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getDashboard()
      .then(setData)
      .catch((e: unknown) => {
        // A 30-day token outlives most things, but not a rotated Jwt:SecretKey.
        if (e instanceof ApiError && e.status === 401) {
          onUnauthorized();
          return;
        }

        setError(e instanceof Error ? e.message : 'Could not load the dashboard');
      });
  }, [onUnauthorized]);

  if (error) {
    return <p className="error">{error}</p>;
  }

  if (!data) {
    return <p className="empty">Loading…</p>;
  }

  return (
    <>
      <p className="today">{data.today}</p>

      <div className="buckets">
        <Bucket title="Overdue" tasks={data.overdue} />
        <Bucket title="Today" tasks={data.dueToday} />
        <Bucket title="Upcoming" tasks={data.upcoming} />
        <Bucket title="Done today" tasks={data.completedToday} />
      </div>

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
