import { useCallback, useState } from 'react';
import { clearWeeklyDish, saveRecipes, searchRecipes, setWeeklyDish } from '../api';
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
  // The query the results on screen came from. Paging has to run on this rather
  // than on whatever is half-typed in the box.
  const [submitted, setSubmitted] = useState('');
  const [candidates, setCandidates] = useState<RecipeCandidate[] | null>(null);
  // Whole candidates, not ids: page two replaces the list on screen, and the
  // save has to still know what was ticked on page one.
  const [selected, setSelected] = useState<RecipeCandidate[]>([]);
  const [offset, setOffset] = useState(0);
  const [hasMore, setHasMore] = useState(false);
  const [pageSize, setPageSize] = useState(0);
  const [saved, setSaved] = useState<RecipeHistoryItem[]>([]);
  // Which saved dish is now this week's — separate from `busy`, so changing
  // your mind and cooking a different one instead still works.
  const [cooked, setCooked] = useState<string | null>(null);
  // Which result is expanded to its full ingredients and method.
  const [open, setOpen] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  /** One page of one query. Both the form and the pager come through here. */
  async function run(term: string, at: number) {
    setBusy(true);
    setError(null);

    try {
      const page = await searchRecipes(term, at);

      setCandidates(page.candidates);
      setHasMore(page.hasMore);
      setPageSize(page.pageSize);
      setOffset(at);
      setSubmitted(term);
      // Whatever was open belonged to the page being replaced.
      setOpen(null);
    } catch (e) {
      fail(e);
    } finally {
      setBusy(false);
    }
  }

  async function search(e: React.FormEvent) {
    e.preventDefault();

    if (query.trim() === '') {
      return;
    }

    setSaved([]);
    setSelected([]);

    await run(query.trim(), 0);
  }

  function toggle(candidate: RecipeCandidate) {
    setSelected((current) =>
      current.some((c) => c.externalId === candidate.externalId)
        ? current.filter((c) => c.externalId !== candidate.externalId)
        : [...current, candidate],
    );
  }

  async function save() {
    setBusy(true);
    setError(null);

    try {
      setSaved(await saveRecipes(selected));
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

  async function undo() {
    setBusy(true);
    setError(null);

    try {
      await clearWeeklyDish();
      setCooked(null);
    } catch (e) {
      fail(e);
    } finally {
      setBusy(false);
    }
  }

  // pageSize is 0 until the first response, which is also the only time this is
  // not on screen.
  const pageNumber = pageSize > 0 ? Math.floor(offset / pageSize) + 1 : 1;

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
                    <>
                      <span className="notes">Now this week's dish.</span>
                      <button type="button" className="link" disabled={busy} onClick={undo}>
                        Undo
                      </button>
                    </>
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
              Page {pageNumber} · {selected.length} selected
            </p>

            <button type="button" disabled={busy || selected.length === 0} onClick={save}>
              {busy ? 'Saving…' : `Save ${selected.length} to library`}
            </button>
          </div>

          {candidates.map((candidate) => (
            <section key={candidate.externalId} className="card">
              {/* Tapping the dish opens it — the gesture people try first, and
                  on a tablet a link the size of two words is a poor target. The
                  checkbox swallows its own clicks: ticking one to save it and
                  reading it are different intents. The expanded method sits
                  outside this, so scrolling it does not fold the card shut. */}
              <div
                className="candidate-head"
                onClick={() => setOpen(open === candidate.externalId ? null : candidate.externalId)}
              >
                <div className="candidate-title">
                  <label className="checkbox" onClick={(e) => e.stopPropagation()}>
                    <input
                      type="checkbox"
                      aria-label={`Select ${candidate.title}`}
                      checked={selected.some((c) => c.externalId === candidate.externalId)}
                      onChange={() => toggle(candidate)}
                    />
                  </label>

                  <span className="title">{candidate.title}</span>

                  <span className="notes">{open === candidate.externalId ? 'Hide' : 'Read it'}</span>
                </div>

                {/* Joined rather than concatenated: catalog dishes have no time
                    or servings, and the old version left a dangling "·". */}
                <p className="notes">
                  {[
                    candidate.readyMinutes !== null ? `${candidate.readyMinutes} min` : null,
                    candidate.servings !== null ? `serves ${candidate.servings}` : null,
                    `${candidate.ingredients.length} ingredients`,
                    `${candidate.steps.length} steps`,
                  ]
                    .filter((part) => part !== null)
                    .join(' · ')}
                </p>

                {/* The whole point of this line: the corpus has three dishes all
                    called "Chicken Ramen", and the ingredients are the only thing
                    that tells them apart. */}
                {candidate.ingredients.length > 0 && (
                  <p className="ingredient-peek">
                    {candidate.ingredients
                      .slice(0, 6)
                      .map((i) => i.item)
                      .join(', ')}
                    {candidate.ingredients.length > 6 && ` … +${candidate.ingredients.length - 6} more`}
                  </p>
                )}
              </div>

              {open === candidate.externalId && (
                <div className="buckets">
                  <section className="card">
                    <h2>
                      Ingredients <span className="count">{candidate.ingredients.length}</span>
                    </h2>
                    <ul>
                      {candidate.ingredients.map((ingredient, index) => (
                        <li key={index}>
                          <span className="title">{ingredient.item}</span>
                        </li>
                      ))}
                    </ul>
                  </section>

                  <section className="card">
                    <h2>
                      Method <span className="count">{candidate.steps.length}</span>
                    </h2>
                    <ol className="steps">
                      {candidate.steps.map((step, index) => (
                        <li key={index}>{step}</li>
                      ))}
                    </ol>
                  </section>
                </div>
              )}

              {candidate.imageUrl && <img className="dish-image" src={candidate.imageUrl} alt="" />}
            </section>
          ))}

          {/* Selections survive paging, so wandering off page one to look for
              something better costs nothing. */}
          <div className="toolbar">
            <button
              type="button"
              className="ghost"
              disabled={busy || offset === 0}
              onClick={() => run(submitted, Math.max(0, offset - pageSize))}
            >
              ‹ Previous
            </button>

            <p className="today">Page {pageNumber}</p>

            <button
              type="button"
              className="ghost"
              disabled={busy || !hasMore}
              onClick={() => run(submitted, offset + pageSize)}
            >
              Next ›
            </button>
          </div>
        </>
      )}
    </>
  );
}
