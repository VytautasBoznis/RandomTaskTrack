import { useState } from 'react';
import Credentials from './Credentials';
import LearningPaths from './LearningPaths';

type Pane = 'paths' | 'achieved';

const PANES: { id: Pane; label: string }[] = [
  { id: 'paths', label: 'Paths' },
  { id: 'achieved', label: 'Achieved' },
];

export default function Learning({ onUnauthorized }: { onUnauthorized: () => void }) {
  const [pane, setPane] = useState<Pane>('paths');

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

      {/* Remounting on every switch, as the top-level nav does: passing an exam
          on Paths has to move it into Achieved. */}
      {pane === 'paths' && <LearningPaths onUnauthorized={onUnauthorized} />}
      {pane === 'achieved' && <Credentials onUnauthorized={onUnauthorized} />}
    </>
  );
}
