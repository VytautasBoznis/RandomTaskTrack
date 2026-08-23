import { useCallback, useState } from 'react';
import Chat from './components/Chat';
import CompletionLog from './components/CompletionLog';
import Dashboard from './components/Dashboard';
import Login from './components/Login';
import Recurrences from './components/Recurrences';
import Register from './components/Register';
import { clearToken, getToken } from './api';

type View = 'today' | 'recurrences' | 'log' | 'chat';

const VIEWS: { id: View; label: string }[] = [
  { id: 'today', label: 'Today' },
  { id: 'recurrences', label: 'Recurring' },
  { id: 'log', label: 'Log' },
  { id: 'chat', label: 'Chat' },
];

export default function App() {
  const [signedIn, setSignedIn] = useState(() => getToken() !== null);
  const [registering, setRegistering] = useState(false);
  const [view, setView] = useState<View>('today');

  // Stable identity: the views use this as an effect dependency.
  const signOut = useCallback(() => {
    clearToken();
    setSignedIn(false);
    setRegistering(false);
  }, []);

  const signIn = () => {
    setSignedIn(true);
    setRegistering(false);
  };

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

      {signedIn ? (
        <>
          <nav>
            {VIEWS.map((item) => (
              <button
                key={item.id}
                type="button"
                className={item.id === view ? 'tab selected' : 'tab'}
                onClick={() => setView(item.id)}
              >
                {item.label}
              </button>
            ))}
          </nav>

          {/* Remounting on every switch is deliberate: each view re-reads on
              mount, which is also how a chat turn's writes reach the dashboard. */}
          {view === 'today' && <Dashboard onUnauthorized={signOut} />}
          {view === 'recurrences' && <Recurrences onUnauthorized={signOut} />}
          {view === 'log' && <CompletionLog onUnauthorized={signOut} />}
          {view === 'chat' && <Chat onUnauthorized={signOut} />}
        </>
      ) : registering ? (
        <Register onSignedIn={signIn} onCancel={() => setRegistering(false)} />
      ) : (
        <Login onSignedIn={signIn} onRegister={() => setRegistering(true)} />
      )}
    </div>
  );
}
