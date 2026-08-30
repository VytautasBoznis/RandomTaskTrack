import { useState } from 'react';
import { deleteCredential, remindCredential, researchCredential } from '../api';
import { RenewalKinds } from '../types';
import type { HeldCredential, LearningGoal } from '../types';

/** "in 47 days" / "8 days ago". Only ever called for something that expires. */
function countdown(days: number): string {
  if (days === 0) return 'today';
  if (days < 0) return `${-days} day${days === -1 ? '' : 's'} ago`;

  if (days < 60) return `in ${days} days`;

  const months = Math.round(days / 30);

  return months < 24 ? `in ${months} months` : `in ${Math.round(days / 365)} years`;
}

export default function CredentialCard({
  credential,
  goals,
  onChanged,
  onReload,
  onEdit,
  fail,
}: {
  credential: HeldCredential;
  goals: LearningGoal[];
  onChanged: (credential: HeldCredential) => void;
  onReload: () => Promise<void>;
  onEdit: () => void;
  fail: (e: unknown) => void;
}) {
  const [busy, setBusy] = useState<string | null>(null);

  const expires = credential.renewalKind === RenewalKinds.Expires;
  const lapsed = expires && (credential.daysUntilExpiry ?? 0) < 0;
  const goal = goals.find((item) => item.id === credential.goalId) ?? null;
  const renewal = credential.renewal;

  async function run(what: string, action: () => Promise<unknown>) {
    setBusy(what);

    try {
      await action();
    } catch (e) {
      fail(e);
    } finally {
      setBusy(null);
    }
  }

  const look = () => run('look', async () => onChanged(await researchCredential(credential.id)));

  const remind = () => run('remind', async () => onChanged(await remindCredential(credential.id, null)));

  const remove = () => {
    if (!window.confirm(`Delete "${credential.name}"?`)) {
      return;
    }

    run('delete', async () => {
      await deleteCredential(credential.id);
      await onReload();
    });
  };

  return (
    <section className="card credential">
      <h2>
        {credential.name}
        {credential.code && <span className="kind">{credential.code}</span>}
      </h2>

      <p className="muted">
        {[credential.issuer, `Earned ${credential.earnedOn}`, goal && `For: ${goal.title}`]
          .filter(Boolean)
          .join(' · ')}
      </p>

      {/* The one line that differs per group, and the reason renewal_kind is a
          tri-state: a permanent credential says so rather than showing an empty
          countdown, and an unchecked one asks rather than implying "never". */}
      {expires ? (
        <p className={lapsed || credential.isRenewable ? 'error' : ''}>
          {lapsed ? 'Lapsed' : 'Expires'} {credential.expiresOn} — {countdown(credential.daysUntilExpiry ?? 0)}
          {credential.isRenewable && !lapsed && ' · renewable now'}
        </p>
      ) : (
        <p className={credential.renewalKind === RenewalKinds.Permanent ? 'muted' : ''}>
          {credential.renewalKind === RenewalKinds.Permanent
            ? 'Does not expire.'
            : 'Nobody has checked whether this expires.'}
        </p>
      )}

      {renewal && (
        <dl className="care">
          {renewal.renewal !== '' && (
            <div>
              <dt>How to renew</dt>
              <dd>{renewal.renewal}</dd>
            </div>
          )}
          {renewal.cost !== '' && (
            <div>
              <dt>Cost</dt>
              <dd>{renewal.cost}</dd>
            </div>
          )}
          {renewal.ifLapsed !== '' && (
            <div>
              <dt>If it lapses</dt>
              <dd>{renewal.ifLapsed}</dd>
            </div>
          )}
          {renewal.notes !== '' && (
            <div>
              <dt>Notes</dt>
              <dd>{renewal.notes}</dd>
            </div>
          )}
          {renewal.officialUrl !== '' && (
            <div>
              <dt>Stated at</dt>
              <dd>
                <a href={renewal.officialUrl} target="_blank" rel="noreferrer">
                  {renewal.officialUrl}
                </a>
              </dd>
            </div>
          )}
        </dl>
      )}

      {credential.notes !== '' && <p>{credential.notes}</p>}

      <div className="actions">
        {/* Offered whatever the kind: on a permanent one it does not change the
            dates, it fills in how the programme actually works. */}
        <button type="button" className="ghost" onClick={look} disabled={busy !== null}>
          {busy === 'look' ? 'Looking up…' : credential.researchedAt ? 'Look up again' : 'Look up renewal'}
        </button>

        {expires &&
          (credential.task ? (
            <span className="muted">Reminder on the board for {credential.task.dueOn}</span>
          ) : (
            <button type="button" className="ghost" onClick={remind} disabled={busy !== null}>
              Remind me
            </button>
          ))}

        <button type="button" className="ghost" onClick={onEdit} disabled={busy !== null}>
          {expires ? 'Edit / renewed' : 'Edit'}
        </button>

        <button type="button" className="danger" onClick={remove} disabled={busy !== null}>
          Delete
        </button>
      </div>

      {credential.researchedAt && (
        <p className="muted drafted">
          Checked {credential.researchedAt.slice(0, 10)}. Issuers change these — confirm before you
          rely on it.
        </p>
      )}
    </section>
  );
}
