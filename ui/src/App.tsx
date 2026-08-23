import { useCallback, useState } from 'react';
import Dashboard from './components/Dashboard';
import Login from './components/Login';
import { clearToken, getToken } from './api';

export default function App() {
  const [signedIn, setSignedIn] = useState(() => getToken() !== null);

  // Stable identity: Dashboard uses this as an effect dependency.
  const signOut = useCallback(() => {
    clearToken();
    setSignedIn(false);
  }, []);

  return (
    <div className="app">
      <header>
        <h1>RandomTaskTrack</h1>
        {signedIn && (
          <button type="button" className="link" onClick={signOut}>
            Sign out
          </button>
        )}
      </header>

      {signedIn ? <Dashboard onUnauthorized={signOut} /> : <Login onSignedIn={() => setSignedIn(true)} />}
    </div>
  );
}
