import { useState } from 'react';
import { createCredential, updateCredential } from '../api';
import { RENEWAL_KIND_LABELS, RenewalKinds } from '../types';
import type { CredentialRenewalKind, HeldCredential, LearningGoal } from '../types';

const KINDS: CredentialRenewalKind[] = [3, 2, 1];

const blank = (value: string) => (value.trim() === '' ? null : value.trim());

export default function CredentialForm({
  credential,
  goals,
  onSaved,
  onCancel,
}: {
  credential: HeldCredential | null;
  goals: LearningGoal[];
  onSaved: () => void;
  onCancel: () => void;
}) {
  const [name, setName] = useState(credential?.name ?? '');
  const [issuer, setIssuer] = useState(credential?.issuer ?? '');
  const [code, setCode] = useState(credential?.code ?? '');
  const [earnedOn, setEarnedOn] = useState(credential?.earnedOn ?? '');
  const [renewalKind, setRenewalKind] = useState<CredentialRenewalKind>(
    credential?.renewalKind ?? RenewalKinds.Unknown,
  );
  const [expiresOn, setExpiresOn] = useState(credential?.expiresOn ?? '');
  const [goalId, setGoalId] = useState(credential?.goalId ?? '');
  const [credentialId, setCredentialId] = useState(credential?.credentialId ?? '');
  const [url, setUrl] = useState(credential?.url ?? '');
  const [notes, setNotes] = useState(credential?.notes ?? '');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const expires = renewalKind === RenewalKinds.Expires;
  const permanent = renewalKind === RenewalKinds.Permanent;

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    const draft = {
      name: name.trim(),
      issuer: blank(issuer),
      code: blank(code),
      earnedOn,
      renewalKind,

      // The rule the database also enforces: a permanent credential must not
      // carry a date. Cleared here rather than rejected, since switching to
      // Permanent is exactly how someone corrects a date they should not have
      // typed.
      expiresOn: permanent ? null : blank(expiresOn),

      goalId: goalId === '' ? null : goalId,
      credentialId: blank(credentialId),
      url: blank(url),
      notes: blank(notes),
    };

    try {
      if (credential) {
        await updateCredential(credential.id, draft);
      } else {
        await createCredential(draft);
      }

      onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not save');
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="task-form" onSubmit={submit}>
      <label>
        Credential
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Microsoft Certified: Azure Solutions Architect Expert"
          required
          maxLength={200}
        />
      </label>

      <div className="row">
        <label>
          Issuer
          <input value={issuer} onChange={(e) => setIssuer(e.target.value)} placeholder="Microsoft" maxLength={200} />
        </label>

        <label>
          Code
          <input value={code} onChange={(e) => setCode(e.target.value)} placeholder="AZ-305" maxLength={50} />
        </label>

        <label>
          Earned
          <input type="date" value={earnedOn} onChange={(e) => setEarnedOn(e.target.value)} required />
        </label>
      </div>

      <div className="row">
        <label>
          Does it expire?
          <select
            value={renewalKind}
            onChange={(e) => setRenewalKind(Number(e.target.value) as CredentialRenewalKind)}
          >
            {KINDS.map((value) => (
              <option key={value} value={value}>
                {RENEWAL_KIND_LABELS[value]}
              </option>
            ))}
          </select>
        </label>

        {/* Only where it means something. A permanent credential has no date to
            give, and offering the box would invite a contradiction. */}
        {!permanent && (
          <label>
            Expires
            <input
              type="date"
              value={expiresOn}
              onChange={(e) => setExpiresOn(e.target.value)}
              required={expires}
            />
          </label>
        )}

        <label>
          Earned for
          <select value={goalId} onChange={(e) => setGoalId(e.target.value)}>
            <option value="">—</option>
            {goals.map((goal) => (
              <option key={goal.id} value={goal.id}>
                {goal.title}
              </option>
            ))}
          </select>
        </label>
      </div>

      <p className="muted">
        {permanent
          ? 'It stays on your record and never needs renewing — older Microsoft and pre-2011 CompTIA certifications are like this.'
          : expires
            ? 'Enter the date it runs out, or leave this as “Not checked” and press Look up renewal to find it.'
            : 'Leave this if you are not sure. “Look up renewal” searches the issuer’s current policy for the date it was earned.'}
      </p>

      <div className="row">
        <label>
          Credential number
          <input
            value={credentialId}
            onChange={(e) => setCredentialId(e.target.value)}
            maxLength={200}
          />
        </label>

        <label>
          Link
          <input value={url} onChange={(e) => setUrl(e.target.value)} maxLength={500} />
        </label>
      </div>

      <label>
        Notes
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={2} maxLength={4000} />
      </label>

      {error && <p className="error">{error}</p>}

      <div className="actions">
        <button type="submit" disabled={busy || name.trim() === '' || earnedOn === ''}>
          {busy ? 'Saving…' : credential ? 'Save' : 'Add credential'}
        </button>
        <button type="button" className="ghost" onClick={onCancel} disabled={busy}>
          Cancel
        </button>
      </div>
    </form>
  );
}
