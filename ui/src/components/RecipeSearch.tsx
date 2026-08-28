import { useCallback, useState } from 'react';
import { saveRecipes, searchRecipes, setWeeklyDish } from '../api';
import { useApiError } from '../hooks';
import RecipeCatalog from './RecipeCatalog';
import type { RecipeCandidate, RecipeHistoryItem } from '../types';

/**
 * Overriding the rotation: search a dish by name, keep the ones worth keeping,
 * cook one of them this week. The rest stay in the library and turn up under
 * History as "not cooked yet", so nothing found here is wasted.
 */
export default function RecipeSearch({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [query, setQuery] = useState('');
  const [candidates, setCandidates] = useState<RecipeCandidate[] | null>(null);
  const [selected, setSelected] = useState<string[]>([]);
  const [saved, setSaved] = useState<RecipeHistoryItem[]>([]);
  // Which saved dish is now this week's — separate from `busy`, so changing
  // your mind and cooking a different one instead still works.
  const [cooked, setCooked] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function search(e: React.FormEvent) {
    e.preventDefault();

    if (query.trim() === '') {
      return;
    }

    setBusy(true);
    setError(null);
    setSaved([]);
    setSelected([]);

    try {
      setCandidates(await searchRecipes(query.trim()));
    } catch (e) {
      fail(e);
    } finally {
      setBusy(false);
    }
  }

  function toggle(externalId: string) {
    setSelected((current) =>
      current.includes(externalId) ? current.filter((id) => id !== externalId) : [...current, externalId],
    );
  }

  async function save() {
    if (candidates === null) {
      return;
    }

    setBusy(true);
    setError(null);

    try {
      setSaved(await saveRecipes(candidates.filter((c) => selected.includes(c.externalId))));
    } catch (e) {
      fail(e);
    } finally {
      setBusy(false);
    }
  }

  async function cook(recipeId: string) {
    setBusy(true);
    setError(null);

    try {
      await setWeeklyDish(recipeId);
      setCooked(recipeId);
    } catch (e) {
      fail(e);
    } finally {
      setBusy(false);
    }
  }

  // An import that has just finished changes what a search would return, so
  // clear the old results rather than leaving stale ones on screen.
  const onCatalogLoaded = useCallback(() => {
    setCandidates(null);
    setSelected([]);
  }, []);

  return (
    <>
      <RecipeCatalog onLoaded={onCatalogLoaded} />

      <form className="toolbar" onSubmit={search}>
        <input
          type="search"
          placeholder="Search a dish — ramen, katsu curry, dal…"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />

        <button type="submit" disabled={busy || query.trim() === ''}>
          {busy ? 'Looking…' : 'Search'}
        </button>
      </form>

      {error && <p className="error">{error}</p>}

      {saved.length > 0 && (
        <section className="card">
          <h2>
            Saved to the library <span className="count">{saved.length}</span>
          </h2>

          <ul>
            {saved.map((recipe) => (
              <li key={recipe.recipeId}>
                <span className="title">{recipe.title}</span>

                <span className="actions">
                  {cooked === recipe.recipeId ? (
                    <span className="notes">This week's dish — see the This week tab.</span>
                  ) : (
                    <button type="button" className="link" disabled={busy} onClick={() => cook(recipe.recipeId)}>
                      Cook this week
                    </button>
                  )}
                </span>
              </li>
            ))}
          </ul>

          <p className="notes">
            The ones you do not cook stay in the library — find them under History, "Not cooked yet".
          </p>
        </section>
      )}

      {candidates === null ? (
        <p className="empty">Search for a dish to override this week's rotation.</p>
      ) : candidates.length === 0 ? (
        <p className="empty">Nothing came back for "{query}".</p>
      ) : (
        <>
          <div className="toolbar">
            <p className="today">
              {candidates.length} found · {selected.length} selected
            </p>

            <button type="button" disabled={busy || selected.length === 0} onClick={save}>
              {busy ? 'Saving…' : `Save ${selected.length} to library`}
            </button>
          </div>

          {candidates.map((candidate) => (
            <section key={candidate.externalId} className="card">
              <label className="checkbox">
                <input
                  type="checkbox"
                  checked={selected.includes(candidate.externalId)}
                  onChange={() => toggle(candidate.externalId)}
                />
                <span className="title">{candidate.title}</span>
              </label>

              <p className="notes">
                {candidate.readyMinutes !== null && `${candidate.readyMinutes} min`}
                {candidate.servings !== null && ` · serves ${candidate.servings}`}
                {` · ${candidate.ingredients.length} ingredients · ${candidate.steps.length} steps`}
              </p>

              {candidate.imageUrl && <img className="dish-image" src={candidate.imageUrl} alt="" />}
            </section>
          ))}
        </>
      )}
    </>
  );
}
