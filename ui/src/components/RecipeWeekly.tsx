import { useCallback, useEffect, useState } from 'react';
import { clearWeeklyDish, createDishTask, getWeeklyDish, rerollDish, updateRecipe } from '../api';
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
  // Distinct from `dish === null`, which is also the state before the first
  // load has come back. Without this the empty week flashes the spinner.
  const [loaded, setLoaded] = useState(false);
  const [families, setFamilies] = useState<RecipeFamily[]>([]);
  const [familyId, setFamilyId] = useState<number | null>(null);
  const [checked, setChecked] = useState<string[]>([]);
  const [dueOn, setDueOn] = useState('');
  const [busy, setBusy] = useState(false);

  const show = useCallback((next: WeeklyDish | null) => {
    setDish(next);
    setChecked(next === null ? [] : loadChecked(next.pickId));
    setLoaded(true);
  }, []);

  useEffect(() => {
    getWeeklyDish()
      .then((response) => {
        show(response.dish);
        setFamilies(response.families);
      })
      .catch(fail);
  }, [fail, show]);

  async function clear() {
    setBusy(true);
    setError(null);

    try {
      await clearWeeklyDish();
      show(null);
    } catch (e) {
      fail(e);
    } finally {
      setBusy(false);
    }
  }

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

  if (!loaded) {
    return error ? <p className="error">{error}</p> : <p className="empty">Finding this week's dish…</p>;
  }

  // Nothing on the board. Reached by clearing a dish, or by the rotation coming
  // up empty — either way it is a normal state with three ways out, not an
  // error, and it stays cleared until something is chosen.
  if (dish === null) {
    return (
      <>
        <div className="toolbar">
          <p className="today">No dish this week.</p>

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

            <button type="button" disabled={busy} onClick={reroll}>
              {busy ? 'Rolling…' : 'Pick one for me'}
            </button>
          </span>
        </div>

        {error && <p className="error">{error}</p>}

        <section className="card">
          <p className="notes">
            Or choose one yourself — <strong>Find a dish</strong> searches by name, and <strong>History</strong>
            {' '}has everything saved but not cooked, with a <strong>Cook this week</strong> on each. Those two need
            no online quota.
          </p>
        </section>
      </>
    );
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

          {/* The way off the board that does not need the rotation. Reroll
              replaces the dish and costs a source call; this just clears it. */}
          <button type="button" className="link" disabled={busy} onClick={clear}>
            Not cooking this
          </button>
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
