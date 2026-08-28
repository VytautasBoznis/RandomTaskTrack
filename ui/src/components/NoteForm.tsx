import { useState, type FormEvent } from 'react';
import { createNote, updateNote } from '../api';
import type { Note } from '../types';

export default function NoteForm({
  note,
  onSaved,
  onCancel,
}: {
  note: Note | null;
  onSaved: () => void;
  onCancel: () => void;
}) {
  const [title, setTitle] = useState(note?.title ?? '');
  const [content, setContent] = useState(note?.content ?? '');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    // Both fields always go, so emptying a note's body actually empties it —
    // null is what the server reads as "leave alone".
    const draft = { title: title.trim(), content };

    try {
      await (note ? updateNote(note.id, draft) : createNote(draft));
      onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not save the note');
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card task-form note-form" onSubmit={submit}>
      <h2>{note ? 'Edit note' : 'New note'}</h2>

      <label>
        Title
        <input value={title} onChange={(e) => setTitle(e.target.value)} required maxLength={500} autoFocus />
      </label>

      <label>
        Body
        <textarea value={content} onChange={(e) => setContent(e.target.value)} rows={12} />
        <small className="notes">
          Markdown: **bold**, # headings, - lists, [text](https://example.com). Bare links become links too.
        </small>
      </label>

      {error && <p className="error">{error}</p>}

      <div className="actions">
        <button type="submit" disabled={busy}>
          {busy ? 'Saving…' : 'Save'}
        </button>
        <button type="button" className="link" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}
