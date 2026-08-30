import { useCallback, useState } from 'react';
import Chat from './components/Chat';
import CompletionLog from './components/CompletionLog';
import Dashboard from './components/Dashboard';
import Finance from './components/Finance';
import Learning from './components/Learning';
import Login from './components/Login';
import Notes from './components/Notes';
import Plants from './components/Plants';
import Recipes from './components/Recipes';
import Recurrences from './components/Recurrences';
import Register from './components/Register';
import { clearToken, getToken } from './api';

type View = 'today' | 'recurrences' | 'recipes' | 'plants' | 'finance' | 'learning' | 'notes' | 'log' | 'chat';

const VIEWS: { id: View; label: string }[] = [
  { id: 'today', label: 'Today' },
  { id: 'recurrences', label: 'Recurring' },
  { id: 'recipes', label: 'Recipes' },
  { id: 'plants', label: 'Plants' },
  { id: 'finance', label: 'Finance' },
  { id: 'learning', label: 'Learning' },
  { id: 'notes', label: 'Notes' },
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
          {view === 'recipes' && <Recipes onUnauthorized={signOut} />}
          {view === 'plants' && <Plants onUnauthorized={signOut} />}
          {view === 'finance' && <Finance onUnauthorized={signOut} />}
          {view === 'learning' && <Learning onUnauthorized={signOut} />}
          {view === 'notes' && <Notes onUnauthorized={signOut} />}
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
