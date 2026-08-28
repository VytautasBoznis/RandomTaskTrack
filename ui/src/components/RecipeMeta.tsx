import { useState } from 'react';
import { NOT_PICKED } from '../types';
import type { RecipeMetaDraft } from '../types';

const STARS = [1, 2, 3, 4, 5];

/** "quick, one pot" -> ["quick", "one pot"]. The server normalises again. */
const parseTags = (value: string) =>
  value
    .split(',')
    .map((tag) => tag.trim().toLowerCase())
    .filter((tag) => tag !== '');

/**
 * The verdict on a dish: stars, what to do differently, tags. Shared by the
 * weekly card and every history row, because it is the same recipe row behind
 * both — rate this week's dish and the rating is there in the cookbook.
 */
export default function RecipeMeta({
  rating,
  notes,
  tags,
  onSave,
}: {
  rating: number | null;
  notes: string;
  tags: string[];
  onSave: (draft: RecipeMetaDraft) => Promise<void>;
}) {
  const [draftRating, setDraftRating] = useState(rating);
  const [draftNotes, setDraftNotes] = useState(notes);
  const [draftTags, setDraftTags] = useState(tags.join(', '));
  const [busy, setBusy] = useState(false);

  const parsed = parseTags(draftTags);
  const skipped = parsed.includes(NOT_PICKED);

  // Rewrites the tag box, so the toggle and typing "not picked" by hand are the
  // same edit — and it is saved by the same button as everything else here.
  function toggleSkip() {
    const next = skipped ? parsed.filter((tag) => tag !== NOT_PICKED) : [...parsed, NOT_PICKED];

    setDraftTags(next.join(', '));
  }

  const dirty =
    draftRating !== rating ||
    draftNotes !== notes ||
    parsed.length !== tags.length ||
    parsed.some((tag, index) => tag !== tags[index]);

  async function save() {
    setBusy(true);

    try {
      await onSave({
        rating: draftRating,
        // Null cannot say "unrate this" on its own — it means "leave alone".
        clearRating: draftRating === null,
        notes: draftNotes,
        tags: parsed,
      });
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="meta">
      <div className="stars">
        {STARS.map((star) => (
          <button
            key={star}
            type="button"
            className={draftRating !== null && star <= draftRating ? 'star on' : 'star'}
            title={`${star} out of 5`}
            // Clicking the current rating again clears it.
            onClick={() => setDraftRating(draftRating === star ? null : star)}
          >
            ★
          </button>
        ))}
        {draftRating === null && <span className="notes">Not rated</span>}
      </div>

      <textarea
        rows={2}
        placeholder="How did it go? Too salty, halve the pancetta…"
        value={draftNotes}
        onChange={(e) => setDraftNotes(e.target.value)}
      />

      <input
        type="text"
        placeholder="Tags, comma separated — quick, one pot, freezes well"
        value={draftTags}
        onChange={(e) => setDraftTags(e.target.value)}
      />

      <div className="actions">
        <button type="button" className="ghost" disabled={!dirty || busy} onClick={save}>
          {busy ? 'Saving…' : dirty ? 'Save notes' : 'Saved'}
        </button>

        <button type="button" className="link" onClick={toggleSkip}>
          {skipped ? 'Put back in the rotation' : 'Never offer this again'}
        </button>

        {skipped && (
          <span className="notes">Stays in the library and in search — just never rerolled.</span>
        )}
      </div>
    </div>
  );
}
