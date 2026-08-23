import { useState, type FormEvent } from 'react';
import { login, register } from '../api';

export default function Register({ onSignedIn, onCancel }: { onSignedIn: () => void; onCancel: () => void }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();

    if (password !== confirm) {
      setError('Passwords do not match');
      return;
    }

    setBusy(true);
    setError(null);

    try {
      // Register returns only the id, so sign in straight after to get a token.
      await register(email, password);
      await login(email, password);
      onSignedIn();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Registration failed');
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card login" onSubmit={submit}>
      <label>
        Email
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoFocus />
      </label>

      <label>
        Password
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          minLength={8}
        />
      </label>

      <label>
        Confirm password
        <input type="password" value={confirm} onChange={(e) => setConfirm(e.target.value)} required />
      </label>

      {error && <p className="error">{error}</p>}

      <button type="submit" disabled={busy}>
        {busy ? 'Creating account…' : 'Create account'}
      </button>

      <p className="switch">
        Already have an account?{' '}
        <button type="button" className="link" onClick={onCancel}>
          Sign in
        </button>
      </p>
    </form>
  );
}
