import type {
  ChatReply,
  CompletionLogItem,
  ConversationDetail,
  ConversationListItem,
  Dashboard,
  Note,
  NoteDraft,
  RecipeFamily,
  Recurrence,
  RecurrenceDraft,
  Session,
  TaskDomain,
  TaskDraft,
  TaskItemStatus,
  TaskListItem,
  WeeklyDish,
} from './types';

// Always relative: in production the ingress routes /api to the API and / here,
// so this is same-origin and CORS never comes into play. In dev the vite proxy
// stands in for the ingress.
const API_BASE = '/api';

const TOKEN_KEY = 'rtt.token';

export const getToken = () => localStorage.getItem(TOKEN_KEY);
export const clearToken = () => localStorage.removeItem(TOKEN_KEY);

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getToken();

  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  });

  if (!response.ok) {
    // ErrorHandlingFilterAttribute returns { message, errorCode, description }.
    const body = await response.json().catch(() => null);
    throw new ApiError(body?.message || `Request failed (${response.status})`, response.status);
  }

  return (await response.json()) as T;
}

export async function login(email: string, password: string): Promise<Session> {
  const { session } = await request<{ session: Session }>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });

  localStorage.setItem(TOKEN_KEY, session.jwtToken);

  return session;
}

// Role is left to the server default (User) — the form never offers it.
export const register = (email: string, password: string) =>
  request<{ userId: string }>('/auth/register', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });

export const getDashboard = async () =>
  (await request<{ dashboard: Dashboard }>('/tasks/dashboard')).dashboard;

export const getDomains = async () => (await request<{ domains: TaskDomain[] }>('/domains')).domains;

export const createTask = (draft: TaskDraft) =>
  request<{ task: TaskListItem }>('/tasks', {
    method: 'POST',
    body: JSON.stringify(draft),
  });

export const updateTask = (id: string, draft: TaskDraft) =>
  request<{ task: TaskListItem }>(`/tasks/${id}`, {
    method: 'PUT',
    body: JSON.stringify(draft),
  });

// No actualData: without domain-specific UI there is nothing to adjust, and the
// server logs the planned payload as the actual one in that case.
export const completeTask = (id: string, status: TaskItemStatus) =>
  request<{ task: TaskListItem }>(`/tasks/${id}/complete`, {
    method: 'POST',
    body: JSON.stringify({ status }),
  });

export const deleteTask = (id: string) =>
  request<{ success: boolean }>(`/tasks/${id}`, { method: 'DELETE' });

export const getCompletionLog = async (domainId: number | null) => {
  const query = domainId === null ? '' : `?domainId=${domainId}`;

  return (await request<{ entries: CompletionLogItem[] }>(`/tasks/completions${query}`)).entries;
};

// Paused recurrences are fetched too — otherwise pausing one would hide it with
// no way to bring it back.
export const getRecurrences = async () =>
  (await request<{ recurrences: Recurrence[] }>('/recurrences?includeInactive=true')).recurrences;

export const createRecurrence = (draft: RecurrenceDraft) =>
  request<{ recurrence: Recurrence; materializedTaskCount: number }>('/recurrences', {
    method: 'POST',
    body: JSON.stringify(draft),
  });

export const updateRecurrence = (id: string, draft: RecurrenceDraft) =>
  request<{ recurrence: Recurrence }>(`/recurrences/${id}`, {
    method: 'PUT',
    body: JSON.stringify(draft),
  });

// Everything the update request leaves null keeps its current value, so this
// pauses or resumes without touching the schedule.
export const setRecurrenceActive = (id: string, isActive: boolean) =>
  request<{ recurrence: Recurrence }>(`/recurrences/${id}`, {
    method: 'PUT',
    body: JSON.stringify({ isActive }),
  });

export const deleteRecurrence = (id: string) =>
  request<{ success: boolean; deletedTaskCount: number }>(`/recurrences/${id}`, { method: 'DELETE' });

// The weekly dish is pulled on first read of the week, so this GET can write.
export const getWeeklyDish = () =>
  request<{ dish: WeeklyDish; families: RecipeFamily[] }>('/recipes/weekly');

export const rerollDish = async (familyId: number | null) =>
  (
    await request<{ dish: WeeklyDish }>('/recipes/reroll', {
      method: 'POST',
      body: JSON.stringify({ familyId }),
    })
  ).dish;

export const createDishTask = (pickId: string, dueOn: string | null) =>
  request<{ task: TaskListItem }>('/recipes/task', {
    method: 'POST',
    body: JSON.stringify({ pickId, dueOn }),
  });

export const getNotes = async () => (await request<{ notes: Note[] }>('/notes')).notes;

export const createNote = (draft: NoteDraft) =>
  request<{ note: Note }>('/notes', {
    method: 'POST',
    body: JSON.stringify(draft),
  });

export const updateNote = (id: string, draft: NoteDraft) =>
  request<{ note: Note }>(`/notes/${id}`, {
    method: 'PUT',
    body: JSON.stringify(draft),
  });

export const deleteNote = (id: string) =>
  request<{ success: boolean }>(`/notes/${id}`, { method: 'DELETE' });

export const getConversations = async () =>
  (await request<{ conversations: ConversationListItem[] }>('/chat/conversations')).conversations;

export const getConversation = async (id: string) =>
  (await request<{ conversation: ConversationDetail }>(`/chat/conversations/${id}`)).conversation;

export const sendChatMessage = (conversationId: string | null, message: string, domainId: number | null) =>
  request<ChatReply>('/chat/messages', {
    method: 'POST',
    body: JSON.stringify({ conversationId, message, domainId }),
  });

export const deleteConversation = (id: string) =>
  request<{ success: boolean }>(`/chat/conversations/${id}`, { method: 'DELETE' });
