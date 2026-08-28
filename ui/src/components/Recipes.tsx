import { useState } from 'react';
import RecipeHistory from './RecipeHistory';
import RecipeSearch from './RecipeSearch';
import RecipeWeekly from './RecipeWeekly';

type Pane = 'weekly' | 'search' | 'history';

const PANES: { id: Pane; label: string }[] = [
  { id: 'weekly', label: 'This week' },
  { id: 'search', label: 'Find a dish' },
  { id: 'history', label: 'History' },
];

export default function Recipes({ onUnauthorized }: { onUnauthorized: () => void }) {
  const [pane, setPane] = useState<Pane>('weekly');

  return (
    <>
      <nav className="sub">
        {PANES.map((item) => (
          <button
            key={item.id}
            type="button"
            className={item.id === pane ? 'tab selected' : 'tab'}
            onClick={() => setPane(item.id)}
          >
            {item.label}
          </button>
        ))}
      </nav>

      {/* Remounting on every switch, as the top-level nav does: cooking a dish
          from search or history has to show up on This week. */}
      {pane === 'weekly' && <RecipeWeekly onUnauthorized={onUnauthorized} />}
      {pane === 'search' && <RecipeSearch onUnauthorized={onUnauthorized} />}
      {pane === 'history' && <RecipeHistory onUnauthorized={onUnauthorized} />}
    </>
  );
}
