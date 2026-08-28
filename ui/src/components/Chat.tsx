import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { deleteConversation, getConversation, getConversations, sendChatMessage } from '../api';
import { useApiError, useDomains } from '../hooks';
import type { AppliedToolCall, ConversationDetail, ConversationListItem } from '../types';

/** The stored tool calls are raw provider JSON; only the names are worth showing. */
function toolNames(json: string | null) {
  if (!json) {
    return [];
  }

  try {
    const calls = JSON.parse(json) as { Name?: string; name?: string }[];

    return calls.map((call) => call.Name ?? call.name ?? '').filter(Boolean);
  } catch {
    return [];
  }
}

export default function Chat({ onUnauthorized }: { onUnauthorized: () => void }) {
  const { error, setError, fail } = useApiError(onUnauthorized);
  const domains = useDomains(fail);
  const [conversations, setConversations] = useState<ConversationListItem[]>([]);
  const [active, setActive] = useState<ConversationDetail | null>(null);
  const [domainId, setDomainId] = useState<number | null>(null);
  const [message, setMessage] = useState('');
  const [applied, setApplied] = useState<AppliedToolCall[]>([]);
  const [busy, setBusy] = useState(false);

  const reloadList = useCallback(() => getConversations().then(setConversations).catch(fail), [fail]);

  useEffect(() => {
    reloadList();
  }, [reloadList]);

  function startNewChat() {
    setActive(null);
    setApplied([]);
    setMessage('');
    setError(null);
  }

  async function open(id: string) {
    setApplied([]);
    setError(null);

    try {
      setActive(await getConversation(id));
    } catch (e) {
      fail(e);
    }
  }

  async function remove(conversation: ConversationListItem) {
    if (!window.confirm(`Delete "${conversation.title}"?`)) {
      return;
    }

    try {
      await deleteConversation(conversation.id);

      if (active?.id === conversation.id) {
        setActive(null);
      }

      await reloadList();
    } catch (e) {
      fail(e);
    }
  }

  async function send(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    try {
      // A turn can create or complete tasks, so re-read the conversation
      // afterwards rather than appending the reply locally.
      const reply = await sendChatMessage(active?.id ?? null, message, domainId);

      setMessage('');
      setApplied(reply.appliedToolCalls);
      setActive(await getConversation(reply.conversationId));
      await reloadList();
    } catch (e) {
      fail(e);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="chat">
      <section className="card conversations">
        <div className="head">
          <h2>Chats</h2>
          <button type="button" disabled={busy} onClick={startNewChat}>
            + New
          </button>
        </div>

        <ul>
          {/* The draft has no row of its own server-side: nothing is stored until
              the first message, which is also what names it. */}
          {active === null && (
            <li className="selected">
              <span className="pick">
                <span className="name">New chat</span>
                <span className="when">Named after your first message</span>
              </span>
            </li>
          )}

          {conversations.map((conversation) => (
            <li key={conversation.id} className={active?.id === conversation.id ? 'selected' : undefined}>
              <button
                type="button"
                className="pick"
                title={conversation.title}
                disabled={busy}
                onClick={() => open(conversation.id)}
              >
                <span className="name">{conversation.title}</span>
                <span className="when">
                  {conversation.messageCount} messages · {new Date(conversation.updatedAt).toLocaleDateString()}
                </span>
              </button>
              <button
                type="button"
                className="link remove"
                title={`Delete "${conversation.title}"`}
                disabled={busy}
                onClick={() => remove(conversation)}
              >
                ✕
              </button>
            </li>
          ))}
        </ul>
      </section>

      <section className="card thread">
        {active === null ? (
          <p className="empty">New chat. Ask for a plan, or tell it what you did.</p>
        ) : (
          active.messages.map((entry) => (
            <div key={entry.id} className={`message ${entry.role}`}>
              {entry.content && <p>{entry.content}</p>}
              {toolNames(entry.toolCalls).length > 0 && (
                <p className="notes">Used: {toolNames(entry.toolCalls).join(', ')}</p>
              )}
            </div>
          ))
        )}

        {busy && <p className="empty">Thinking…</p>}

        {applied.length > 0 && (
          <ul className="applied">
            {applied.map((call, index) => (
              <li key={index} className={call.isError ? 'error' : undefined}>
                <span className="domain">{call.name}</span>
                <span className="title">{call.result}</span>
              </li>
            ))}
          </ul>
        )}

        {error && <p className="error">{error}</p>}

        <form className="composer" onSubmit={send}>
          <select
            value={domainId ?? ''}
            onChange={(e) => setDomainId(e.target.value === '' ? null : Number(e.target.value))}
          >
            <option value="">No domain</option>
            {domains.map((domain) => (
              <option key={domain.id} value={domain.id}>
                {domain.name}
              </option>
            ))}
          </select>

          <input
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            placeholder="Ask or tell…"
            required
            maxLength={20000}
          />

          <button type="submit" disabled={busy}>
            Send
          </button>
        </form>
      </section>
    </div>
  );
}
