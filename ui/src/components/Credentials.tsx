import { useCallback, useEffect, useState } from 'react';
import { getLearning } from '../api';
import { useApiError } from '../hooks';
import CredentialCard from './CredentialCard';
import CredentialForm from './CredentialForm';
import { RenewalKinds } from '../types';
import type { HeldCredential, LearningGoal } from '../types';

export default function Credentials({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const [credentials, setCredentials] = useState<HeldCredential[] | null>(null);
  const [goals, setGoals] = useState<LearningGoal[]>([]);
  const [editing, setEditing] = useState<HeldCredential | 'new' | null>(null);

  const reload = useCallback(
    () =>
      getLearning()
        .then((overview) => {
          setCredentials(overview.credentials);
          setGoals(overview.goals);
        })
        .catch(fail),
    [fail],
  );

  useEffect(() => {
    reload();
  }, [reload]);

  const replace = (saved: HeldCredential) =>
    setCredentials((current) => (current ?? []).map((item) => (item.id === saved.id ? saved : item)));

  async function saved() {
    setEditing(null);
    setError(null);

    // Not replace(): a new credential is not in the list, and one whose expiry
    // moved sorts somewhere else.
    await reload();
  }

  if (!credentials) {
    return error ? <p className="error">{error}</p> : <p className="empty">Loading…</p>;
  }

  // Three groups, because a permanent credential is not a quiet expiring one
  // and should never be read as "expires: never checked". The server sorts by
  // expiry already, so each group keeps that order.
  const expiring = credentials.filter((item) => item.renewalKind === RenewalKinds.Expires);
  const unchecked = credentials.filter((item) => item.renewalKind === RenewalKinds.Unknown);
  const permanent = credentials.filter((item) => item.renewalKind === RenewalKinds.Permanent);

  const group = (title: string, items: HeldCredential[], hint?: string) =>
    items.length === 0 ? null : (
      <>
        <div className="group-head">
          <h2>{title}</h2>
          {hint && <span className="muted">{hint}</span>}
        </div>

        {items.map((credential) => (
          <CredentialCard
            key={credential.id}
            credential={credential}
            goals={goals}
            onChanged={replace}
            onReload={reload}
            onEdit={() => setEditing(credential)}
            fail={fail}
          />
        ))}
      </>
    );

  return (
    <>
      <div className="toolbar">
        <p className="today">
          What you already hold. Renewal rules differ by issuer and change over time, so they get
          looked up per credential rather than assumed — and plenty of credentials never expire at
          all.
        </p>
        <button type="button" onClick={() => setEditing('new')}>
          Add credential
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      {editing && (
        <CredentialForm
          key={editing === 'new' ? 'new' : editing.id}
          credential={editing === 'new' ? null : editing}
          goals={goals}
          onSaved={saved}
          onCancel={() => setEditing(null)}
        />
      )}

      {credentials.length === 0 && <p className="empty">Nothing recorded yet.</p>}

      {group('On a clock', expiring, 'soonest first')}
      {group('Renewal not checked', unchecked, 'look it up, or say so by hand')}
      {group('Permanent', permanent, 'yours for good')}
    </>
  );
}
