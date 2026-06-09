import type { AuthUser, Dashboard, MenuCategory, SessionDetail, TableCard } from './types';

const AUTH_KEY = 'restaurantos_auth';

export function getStoredAuth(): AuthUser | null {
  const raw = localStorage.getItem(AUTH_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
}

export function storeAuth(user: AuthUser) {
  localStorage.setItem(AUTH_KEY, JSON.stringify(user));
}

export function clearAuth() {
  localStorage.removeItem(AUTH_KEY);
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const auth = getStoredAuth();
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  };
  if (auth?.token) headers.Authorization = `Bearer ${auth.token}`;

  const res = await fetch(`/api${path}`, { ...options, headers });
  if (res.status === 401) {
    clearAuth();
    window.location.href = '/login';
    throw new Error('Oturum sona erdi.');
  }
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(data.error || 'İstek başarısız.');
  return data as T;
}

export const api = {
  login: (username: string, password: string) =>
    request<AuthUser>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    }),

  getSections: () => request<string[]>('/floor/sections'),

  getTables: (section?: string) =>
    request<TableCard[]>(`/floor/tables${section ? `?section=${encodeURIComponent(section)}` : ''}`),

  getMenu: () => request<MenuCategory[]>('/menu/categories'),

  getDashboard: () => request<Dashboard>('/dashboard'),

  getSession: (id: string) => request<SessionDetail>(`/sessions/${id}`),

  openSession: (tableId: string, guestCount: number) =>
    request<SessionDetail>('/sessions/open', {
      method: 'POST',
      body: JSON.stringify({ tableId, guestCount }),
    }),

  addOrder: (sessionId: string, menuItemId: string, quantity = 1) =>
    request<SessionDetail>(`/sessions/${sessionId}/orders`, {
      method: 'POST',
      body: JSON.stringify({ menuItemId, quantity }),
    }),

  removeOrder: (sessionId: string, orderItemId: string) =>
    request<SessionDetail>(`/sessions/${sessionId}/orders/${orderItemId}`, { method: 'DELETE' }),

  requestBill: (sessionId: string) =>
    request<SessionDetail>(`/sessions/${sessionId}/bill`, { method: 'POST' }),

  pay: (sessionId: string, amount: number, method: string, changeGiven = 0) =>
    request<SessionDetail>(`/sessions/${sessionId}/payments`, {
      method: 'POST',
      body: JSON.stringify({ amount, method, changeGiven }),
    }),

  cancelSession: (sessionId: string) =>
    request<{ success: boolean }>(`/sessions/${sessionId}/cancel`, { method: 'POST' }),
};

export function formatCurrency(n: number) {
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(n);
}
