import { useCallback, useEffect, useState } from 'react';
import { getPlants } from '../api';
import { useApiError } from '../hooks';
import PlantCard from './PlantCard';
import PlantForm from './PlantForm';
import type { Plant } from '../types';

export default function Plants({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [plants, setPlants] = useState<Plant[] | null>(null);
  const [editing, setEditing] = useState<Plant | 'new' | null>(null);
  // A lookup that failed is not an error the tab is in — the plant is there and
  // the card can retry — so it says so once and gets out of the way.
  const [notice, setNotice] = useState<string | null>(null);

  const reload = useCallback(() => getPlants().then(setPlants).catch(fail), [fail]);

  useEffect(() => {
    reload();
  }, [reload]);

  // The write already returned the whole plant. Swapping it in beats re-reading
  // the list: a lookup takes seconds, and the tab should not blank while one
  // card catches up.
  const replace = (saved: Plant) =>
    setPlants((current) => (current ?? []).map((plant) => (plant.id === saved.id ? saved : plant)));

  async function saved(plant: Plant, researchError: string | null) {
    setEditing(null);
    setNotice(researchError === null ? null : `Added "${plant.name}", but the lookup failed: ${researchError}`);
    setError(null);

    // Not replace(): a new plant is not in the list yet, and a renamed one
    // moves, since the list is alphabetical.
    await reload();
  }

  if (!plants) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  return (
    <>
      <div className="toolbar">
        <p className="today">
          Photograph a plant or a seed packet, and it gets looked up: what it is, and what it needs.
        </p>
        <button type="button" onClick={() => setEditing('new')}>
          Add plant
        </button>
      </div>

      {error && <p className="error">{error}</p>}
      {notice && <p className="error">{notice}</p>}

      {editing && (
        <PlantForm
          key={editing === 'new' ? 'new' : editing.id}
          plant={editing === 'new' ? null : editing}
          onSaved={saved}
          onCancel={() => setEditing(null)}
        />
      )}

      {plants.length === 0 ? (
        <p className="empty">No plants yet.</p>
      ) : (
        plants.map((plant) => (
          <PlantCard
            key={plant.id}
            plant={plant}
            onChanged={replace}
            onReload={reload}
            onEdit={() => setEditing(plant)}
            fail={fail}
          />
        ))
      )}
    </>
  );
}
