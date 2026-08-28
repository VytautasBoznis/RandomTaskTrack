import { useCallback, useEffect, useState } from 'react';
import { createDishTask, getWeeklyDish, rerollDish, updateRecipe } from '../api';
import { useApiError } from '../hooks';
import RecipeMeta from './RecipeMeta';
import type { RecipeFamily, RecipeMetaDraft, WeeklyDish } from '../types';

/** Ticked-off shopping lines, per dish. Local: it is a shopping aid, not history. */
const checkedKey = (pickId: string) => `rtt.shopping.${pickId}`;

function loadChecked(pickId: string): string[] {
  try {
    return JSON.parse(localStorage.getItem(checkedKey(pickId)) ?? '[]') as string[];
  } catch {
    return [];
  }
}

export default function RecipeWeekly({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [dish, setDish] = useState<WeeklyDish | null>(null);
  const [families, setFamilies] = useState<RecipeFamily[]>([]);
  const [familyId, setFamilyId] = useState<number | null>(null);
  const [checked, setChecked] = useState<string[]>([]);
  const [dueOn, setDueOn] = useState('');
  const [busy, setBusy] = useState(false);

  const show = useCallback((next: WeeklyDish) => {
    setDish(next);
    setChecked(loadChecked(next.pickId));
  }, []);

  useEffect(() => {
    getWeeklyDish()
      .then((response) => {
        show(response.dish);
        setFamilies(response.families);
      })
      .catch(fail);
  }, [fail, show]);

  function toggle(item: string) {
    if (dish === null) {
      return;
    }

    const next = checked.includes(item) ? checked.filter((c) => c !== item) : [...checked, item];

    setChecked(next);
    localStorage.setItem(checkedKey(dish.pickId), JSON.stringify(next));
  }

  async function reroll() {
    setBusy(true);
    setError(null);

    try {
      show(await rerollDish(familyId));
    } catch (e) {
      fail(e);
    } finally {
      setBusy(false);
    }
  }

  async function addToTasks() {
    if (dish === null) {
      return;
    }

    setBusy(true);
    setError(null);

    try {
      const { task } = await createDishTask(dish.pickId, dueOn === '' ? null : dueOn);
      setDish({ ...dish, taskId: task.id });
    } catch (e) {
      fail(e);
    } finally {
      setBusy(false);
    }
  }

  async function saveMeta(draft: RecipeMetaDraft) {
    if (dish === null) {
      return;
    }

    setError(null);

    try {
      const saved = await updateRecipe(dish.recipeId, draft);
      setDish({ ...dish, rating: saved.rating, notes: saved.notes, tags: saved.tags });
    } catch (e) {
      fail(e);
    }
  }

  if (!dish) {
    return error ? <p className="error">{error}</p> : <p className="empty">Finding this week's dish…</p>;
  }

  return (
    <>
      <div className="toolbar">
        <p className="today">
          Week of {dish.weekOf}
          {dish.familyName && ` · ${dish.familyName}`}
          {dish.readyMinutes !== null && ` · ${dish.readyMinutes} min`}
          {dish.servings !== null && ` · serves ${dish.servings}`}
        </p>

        <span className="actions">
          <select
            value={familyId ?? ''}
            onChange={(e) => setFamilyId(e.target.value === '' ? null : Number(e.target.value))}
          >
            <option value="">Next in rotation</option>
            {families.map((family) => (
              <option key={family.id} value={family.id}>
                {family.name}
              </option>
            ))}
          </select>

          <button type="button" className="ghost" disabled={busy} onClick={reroll}>
            {busy ? 'Rolling…' : 'Reroll'}
          </button>
        </span>
      </div>

      {error && <p className="error">{error}</p>}

      <section className="card dish">
        <h2>{dish.title}</h2>

        {dish.imageUrl && <img className="dish-image" src={dish.imageUrl} alt="" />}

        <div className="actions">
          {dish.taskId === null ? (
            <>
              <input type="date" value={dueOn} onChange={(e) => setDueOn(e.target.value)} />
              <button type="button" disabled={busy} onClick={addToTasks}>
                Add to tasks
              </button>
              <span className="notes">Defaults to the end of this week.</span>
            </>
          ) : (
            <span className="notes">On the board — it will show up under Today when it is due.</span>
          )}

          {dish.sourceUrl && (
            <a className="notes" href={dish.sourceUrl} target="_blank" rel="noreferrer">
              Original recipe ↗
            </a>
          )}
        </div>

        {/* Keyed on the recipe so a reroll resets the editor rather than
            carrying the previous dish's half-typed notes over. */}
        <RecipeMeta
          key={dish.recipeId}
          rating={dish.rating}
          notes={dish.notes}
          tags={dish.tags}
          onSave={saveMeta}
        />
      </section>

      <div className="buckets">
        <section className="card">
          <h2>
            Shopping list <span className="count">{dish.ingredients.length}</span>
          </h2>

          <ul>
            {dish.ingredients.map((ingredient) => (
              <li key={ingredient.item}>
                <label className="checkbox">
                  <input
                    type="checkbox"
                    checked={checked.includes(ingredient.item)}
                    onChange={() => toggle(ingredient.item)}
                  />
                  <span className={checked.includes(ingredient.item) ? 'title done' : 'title'}>{ingredient.item}</span>
                </label>
              </li>
            ))}
          </ul>
        </section>

        <section className="card">
          <h2>
            Method <span className="count">{dish.steps.length}</span>
          </h2>

          <ol className="steps">
            {dish.steps.map((step, index) => (
              <li key={index}>{step}</li>
            ))}
          </ol>
        </section>
      </div>
    </>
  );
}
