import type { Dashboard, Session } from './types';

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

export const getDashboard = () => request<Dashboard>('/tasks/dashboard');
