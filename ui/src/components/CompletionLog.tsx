import { useEffect, useState } from 'react';
import { getCompletionLog } from '../api';
import { useApiError, useDomains } from '../hooks';
import { TaskStatus } from '../types';
import type { CompletionLogItem } from '../types';

export default function CompletionLog({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, fail } = useApiError(onUnauthorized);
  const domains = useDomains(fail);
  const [domainId, setDomainId] = useState<number | null>(null);
  const [entries, setEntries] = useState<CompletionLogItem[] | null>(null);

  useEffect(() => {
    getCompletionLog(domainId).then(setEntries).catch(fail);
  }, [domainId, fail]);

  if (!entries) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  return (
    <>
      <div className="toolbar">
        <p className="today">What actually happened, newest first.</p>
        <select
          value={domainId ?? ''}
          onChange={(e) => setDomainId(e.target.value === '' ? null : Number(e.target.value))}
        >
          <option value="">All domains</option>
          {domains.map((domain) => (
            <option key={domain.id} value={domain.id}>
              {domain.name}
            </option>
          ))}
        </select>
      </div>

      {error && <p className="error">{error}</p>}

      <section className="card">
        <h2>
          Completions <span className="count">{entries.length}</span>
        </h2>

        {entries.length === 0 ? (
          <p className="empty">Nothing logged yet.</p>
        ) : (
          <ul>
            {entries.map((entry) => (
              <li key={entry.id}>
                <span className="domain">{entry.domainCode}</span>
                <span className="title">
                  {entry.title}
                  {entry.note && <small className="notes">{entry.note}</small>}
                  {/* planned vs actual is the point of this table, so show the
                      payload whenever there is one rather than only the tick. */}
                  {entry.actualData !== '{}' && (
                    <small className="notes">
                      <code>{entry.actualData}</code>
                      {entry.actualData !== entry.plannedData && ' · adjusted from plan'}
                    </small>
                  )}
                </span>
                <span className="due">{entry.status === TaskStatus.Skipped ? 'Skipped' : 'Done'}</span>
                <span className="due">{new Date(entry.completedAt).toLocaleString()}</span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </>
  );
}
