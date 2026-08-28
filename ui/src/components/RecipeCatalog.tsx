import { useCallback, useEffect, useRef, useState } from 'react';
import { getCatalogStatus, startCatalogImport } from '../api';
import type { CatalogStatus } from '../types';

const n = (value: number) => value.toLocaleString();

/**
 * The bulk catalog panel. Sits above search because that is the thing it
 * changes: Spoonacular is thin outside Western food (ramen 5, pad thai 0),
 * the catalog is not (1,061 and 490).
 *
 * The import runs server-side and takes minutes, so this starts it and then
 * polls — nothing here holds a request open, and closing the tab does not stop
 * the run.
 */
export default function RecipeCatalog({ onLoaded }: { onLoaded: () => void }) {
  const [status, setStatus] = useState<CatalogStatus | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const wasRunning = useRef(false);

  const refresh = useCallback(async () => {
    try {
      const next = await getCatalogStatus();
      setStatus(next);

      // Tell the parent once, on the transition, so a finished import refreshes
      // whatever is on screen without polling forever.
      if (wasRunning.current && !next.isRunning) {
        onLoaded();
      }

      wasRunning.current = next.isRunning;
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not read the catalog status');
    }
  }, [onLoaded]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  // Only poll while something is happening — this is a wall tablet left on all
  // day, not a dashboard.
  useEffect(() => {
    if (!status?.isRunning) {
      return;
    }

    const timer = setInterval(refresh, 2000);

    return () => clearInterval(timer);
  }, [status?.isRunning, refresh]);

  async function start() {
    setBusy(true);
    setError(null);

    try {
      const { status: next } = await startCatalogImport();
      setStatus(next);
      wasRunning.current = next.isRunning;
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not start the import');
    } finally {
      setBusy(false);
    }
  }

  if (status === null) {
    return null;
  }

  const percent =
    status.sourceRows > 0 ? Math.min(100, Math.round((status.rowsRead / status.sourceRows) * 100)) : 0;

  return (
    <section className="card catalog">
      <div className="note-head">
        <h2>Local catalog</h2>

        <span className="actions">
          {status.loaded > 0 && !status.isRunning && (
            <span className="due">{n(status.loaded)} recipes loaded</span>
          )}

          <button type="button" disabled={busy || status.isRunning} onClick={start}>
            {status.isRunning
              ? 'Loading…'
              : status.loaded > 0
                ? 'Check for new'
                : `Load ${n(status.sourceRows)} recipes`}
          </button>
        </span>
      </div>

      {status.isRunning ? (
        <>
          <div className="progress">
            <div className="progress-bar" style={{ width: `${percent}%` }} />
          </div>
          <p className="notes">
            {n(status.rowsRead)} of ~{n(status.sourceRows)} rows read ({percent}%). Runs on the server — you can
            leave this tab.
          </p>
        </>
      ) : status.loaded > 0 ? (
        <p className="notes">
          Search reads this first, falling back to the online source when it finds nothing.
          {status.finishedAt !== null &&
            (status.rowsAdded > 0
              ? ` Last run added ${n(status.rowsAdded)} new.`
              : ' Last run found nothing new.')}
        </p>
      ) : (
        <p className="notes">
          Not loaded. The online source is thin outside Western cooking — "ramen" finds 5 dishes there and 1,061
          here. Roughly 2GB, streamed straight into the database; it takes about ten minutes and runs in the
          background.
        </p>
      )}

      {(error ?? status.error) !== null && <p className="error">{error ?? status.error}</p>}
    </section>
  );
}
