import type { Dashboard, Session, TaskDomain, TaskDraft, TaskItemStatus, TaskListItem } from './types';

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
