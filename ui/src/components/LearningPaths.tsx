import { useCallback, useEffect, useState } from 'react';
import { getLearning } from '../api';
import { useApiError } from '../hooks';
import GoalCard from './GoalCard';
import GoalForm from './GoalForm';
import type { LearningGoal } from '../types';

export default function LearningPaths({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [goals, setGoals] = useState<LearningGoal[] | null>(null);
  const [editing, setEditing] = useState<LearningGoal | 'new' | null>(null);

  // Credentials come back on the same call and are needed here for nothing —
  // the Achieved pane reads them itself. Dropping them costs one field.
  const reload = useCallback(
    () => getLearning().then(({ goals: loaded }) => setGoals(loaded)).catch(fail),
    [fail],
  );

  useEffect(() => {
    reload();
  }, [reload]);

  // The write already returned the whole goal. Swapping it in beats re-reading
  // the list: a draft takes the better part of a minute, and the tab should not
  // blank while one card catches up.
  const replace = (saved: LearningGoal) =>
    setGoals((current) => (current ?? []).map((goal) => (goal.id === saved.id ? saved : goal)));

  async function saved() {
    setEditing(null);
    setError(null);

    // Not replace(): a new goal is not in the list yet, and a re-tiered one
    // moves, since the list is ordered by tier.
    await reload();
  }

  if (!goals) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  return (
    <>
      <div className="toolbar">
        <p className="today">
          Say where you want to get to and why. The path gets researched: the phases, the
          certifications worth sitting, what to study with, and projects to build.
        </p>
        <button type="button" onClick={() => setEditing('new')}>
          Add path
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {editing && (
        <GoalForm
          key={editing === 'new' ? 'new' : editing.id}
          goal={editing === 'new' ? null : editing}
          onSaved={saved}
          onCancel={() => setEditing(null)}
        />
      )}

      {goals.length === 0 ? (
        <p className="empty">No paths yet.</p>
      ) : (
        goals.map((goal) => (
          <GoalCard
            key={goal.id}
            goal={goal}
            onChanged={replace}
            onReload={reload}
            onEdit={() => setEditing(goal)}
            fail={fail}
          />
        ))
      )}
    </>
  );
}
