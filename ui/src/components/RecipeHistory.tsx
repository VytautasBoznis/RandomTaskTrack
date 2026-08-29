import { useCallback, useEffect, useState } from 'react';
import { clearWeeklyDish, getRecipeHistory, setWeeklyDish, updateRecipe } from '../api';
import { useApiError } from '../hooks';
import RecipeMeta from './RecipeMeta';
import { NOT_PICKED } from '../types';
import type { RecipeHistoryItem, RecipeMetaDraft } from '../types';

/** null is "everything", which is also the only place this week's dish shows. */
const FILTERS: { label: string; cooked: boolean | null }[] = [
  { label: 'All', cooked: null },
  { label: 'Cooked', cooked: true },
  { label: 'Not cooked yet', cooked: false },
];

const parseTags = (value: string) =>
  value
    .split(',')
    .map((tag) => tag.trim().toLowerCase())
    .filter((tag) => tag !== '');

export default function RecipeHistory({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [entries, setEntries] = useState<RecipeHistoryItem[] | null>(null);
  const [search, setSearch] = useState('');
  const [tags, setTags] = useState('');
  const [cooked, setCooked] = useState<boolean | null>(null);
  const [expanded, setExpanded] = useState<string | null>(null);
  const [cooking, setCooking] = useState<string | null>(null);
  // Which dish this visit put on the board. The reload takes its "Cook this
  // week" away, so without this a misclick has nothing left to click.
  const [justCooked, setJustCooked] = useState<string | null>(null);

  // Filters are applied on submit rather than per keystroke: this is a wall
  // tablet, and every keystroke would be a round trip.
  const [applied, setApplied] = useState({ search: '', tags: [] as string[], cooked: null as boolean | null });

  const reload = useCallback(() => {
    getRecipeHistory(applied.search, applied.tags, applied.cooked).then(setEntries).catch(fail);
  }, [applied, fail]);

  useEffect(() => {
    reload();
  }, [reload]);

  function apply(e: React.FormEvent) {
    e.preventDefault();
    setApplied({ search: search.trim(), tags: parseTags(tags), cooked });
  }

  async function saveMeta(recipeId: string, draft: RecipeMetaDraft) {
    setError(null);

    try {
      const updated = await updateRecipe(recipeId, draft);

      setEntries((current) =>
        (current ?? []).map((entry) => (entry.recipeId === recipeId ? updated : entry)),
      );
    } catch (e) {
      fail(e);
    }
  }

  async function cook(recipeId: string) {
    setCooking(recipeId);
    setError(null);

    try {
      await setWeeklyDish(recipeId);
      setJustCooked(recipeId);
      reload();
    } catch (e) {
      fail(e);
    } finally {
      setCooking(null);
    }
  }

  async function undo(recipeId: string) {
    setCooking(recipeId);
    setError(null);

    try {
      await clearWeeklyDish();
      setJustCooked(null);
      reload();
    } catch (e) {
      fail(e);
    } finally {
      setCooking(null);
    }
  }

  return (
    <>
      <form className="toolbar" onSubmit={apply}>
        <input
          type="search"
          placeholder="Search title or notes"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />

        <input
          type="text"
          placeholder="Tags, comma separated"
          value={tags}
          onChange={(e) => setTags(e.target.value)}
        />

        <span className="actions">
          <select
            value={String(cooked)}
            onChange={(e) => setCooked(e.target.value === 'null' ? null : e.target.value === 'true')}
          >
            {FILTERS.map((filter) => (
              <option key={filter.label} value={String(filter.cooked)}>
                {filter.label}
              </option>
            ))}
          </select>

          <button type="submit">Filter</button>
        </span>
      </form>

      {error && <p className="error">{error}</p>}

      {entries === null ? (
        <p className="empty">Loading…</p>
      ) : entries.length === 0 ? (
        <p className="empty">Nothing matches. Dishes land here once their week is over.</p>
      ) : (
        entries.map((entry) => (
          <section key={entry.recipeId} className="card">
            <div className="note-head">
              <h2>{entry.title}</h2>

              <span className="actions">
                <span className="due">
                  {entry.weekOf === null ? 'Not cooked yet' : `Week of ${entry.weekOf}`}
                  {entry.tags.includes(NOT_PICKED) && ' · not in rotation'}
                </span>

                {entry.rating !== null && <span className="stars-read">{'★'.repeat(entry.rating)}</span>}

                <button
                  type="button"
                  className="link"
                  onClick={() => setExpanded(expanded === entry.recipeId ? null : entry.recipeId)}
                >
                  {expanded === entry.recipeId ? 'Close' : 'Rate & note'}
                </button>

                {entry.weekOf === null && (
                  <button
                    type="button"
                    className="link"
                    disabled={cooking !== null}
                    onClick={() => cook(entry.recipeId)}
                  >
                    Cook this week
                  </button>
                )}

                {justCooked === entry.recipeId && (
                  <button
                    type="button"
                    className="link"
                    disabled={cooking !== null}
                    onClick={() => undo(entry.recipeId)}
                  >
                    Undo
                  </button>
                )}
              </span>
            </div>

            <p className="notes">
              {entry.familyName && `${entry.familyName} · `}
              {entry.readyMinutes !== null && `${entry.readyMinutes} min · `}
              {entry.servings !== null && `serves ${entry.servings} · `}
              {entry.sourceUrl && (
                <a href={entry.sourceUrl} target="_blank" rel="noreferrer">
                  Original recipe ↗
                </a>
              )}
            </p>

            {entry.tags.length > 0 && (
              <p className="tags">
                {entry.tags.map((tag) => (
                  <span key={tag} className="tag">
                    {tag}
                  </span>
                ))}
              </p>
            )}

            {entry.notes.trim() !== '' && expanded !== entry.recipeId && <p>{entry.notes}</p>}

            {expanded === entry.recipeId && (
              <RecipeMeta
                rating={entry.rating}
                notes={entry.notes}
                tags={entry.tags}
                onSave={(draft) => saveMeta(entry.recipeId, draft)}
              />
            )}
          </section>
        ))
      )}
    </>
  );
}
