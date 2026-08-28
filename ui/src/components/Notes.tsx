import { useCallback, useEffect, useState } from 'react';
import { deleteNote, getNotes } from '../api';
import { useApiError } from '../hooks';
import Markdown from './Markdown';
import NoteForm from './NoteForm';
import type { Note } from '../types';

export default function Notes({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [notes, setNotes] = useState<Note[] | null>(null);
  const [editing, setEditing] = useState<Note | 'new' | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const reload = useCallback(() => getNotes().then(setNotes).catch(fail), [fail]);

  useEffect(() => {
    reload();
  }, [reload]);

  async function onDelete(note: Note) {
    if (!window.confirm(`Delete "${note.title}"?`)) {
      return;
    }

    setBusyId(note.id);
    setError(null);

    try {
      await deleteNote(note.id);
      await reload();
    } catch (e) {
      fail(e);
    } finally {
      setBusyId(null);
    }
  }

  if (!notes) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  return (
    <>
      <div className="toolbar">
        <p className="today">Markdown notes, most recently edited first.</p>
        <button type="button" onClick={() => setEditing('new')}>
          New note
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {editing && (
        <NoteForm
          key={editing === 'new' ? 'new' : editing.id}
          note={editing === 'new' ? null : editing}
          onSaved={() => {
            setEditing(null);
            reload();
          }}
          onCancel={() => setEditing(null)}
        />
      )}

      {notes.length === 0 ? (
        <p className="empty">No notes yet.</p>
      ) : (
        notes.map((note) => (
          // A card each rather than list rows: a rendered heading or list needs
          // the room, and squashing it into one line defeats the markdown.
          <section key={note.id} className="card note">
            <div className="note-head">
              <h2>{note.title}</h2>
              <span className="actions">
                <span className="due">{new Date(note.updatedAt).toLocaleString()}</span>
                <button type="button" className="link" onClick={() => setEditing(note)}>
                  Edit
                </button>
                <button type="button" className="link" disabled={busyId === note.id} onClick={() => onDelete(note)}>
                  Delete
                </button>
              </span>
            </div>

            {note.content.trim() === '' ? <p className="empty">Empty.</p> : <Markdown>{note.content}</Markdown>}
          </section>
        ))
      )}
    </>
  );
}
