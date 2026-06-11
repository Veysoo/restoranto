import type {
  AuthUser, Dashboard, KanbanOrder, MenuCategory, MenuItem,
  SalesReport, SessionDetail, TableCard, TableSettings,
} from './types';

const AUTH_KEY = 'restaurantos_auth';

export function getStoredAuth(): AuthUser | null {
  const raw = localStorage.getItem(AUTH_KEY);
  if (!raw) return null;
  try { return JSON.parse(raw) as AuthUser; } catch { return null; }
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
    ...(options.body ? { 'Content-Type': 'application/json' } : {}),
    ...(options.headers as Record<string, string>),
  };
  if (auth?.token) headers.Authorization = `Bearer ${auth.token}`;

  const res = await fetch(`/api${path}`, { ...options, headers });

  if (res.status === 401) {
    clearAuth();
    if (!window.location.pathname.startsWith('/login')) {
      window.location.href = '/login';
    }
    throw new Error('Oturum sona erdi.');
  }

  const text = await res.text();
  const data = text ? JSON.parse(text) : {};
  if (!res.ok) throw new Error(data.error || `Hata (${res.status})`);
  return data as T;
}

export const api = {
  health: () => request<{ status: string }>('/health'),
  networkInfo: () => request<{ ip: string; url: string }>('/health/network'),

  login: async (username: string, password: string): Promise<AuthUser> => {
    const res = await request<AuthUser>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    });
    const user: AuthUser = {
      token: res.token,
      userId: res.userId,
      fullName: res.fullName,
      username: res.username,
      role: res.role,
    };
    storeAuth(user);
    return user;
  },

  getSections: () => request<string[]>('/floor/sections'),
  getTables: (section?: string) =>
    request<TableCard[]>(`/floor/tables${section ? `?section=${encodeURIComponent(section)}` : ''}`),

  getMenu: () => request<MenuCategory[]>('/menu/categories'),
  createCategory: (name: string, icon?: string) =>
    request<MenuCategory>('/menu/categories', { method: 'POST', body: JSON.stringify({ name, icon: icon ?? '🍽️' }) }),
  deleteCategory: (id: string) =>
    request<{ success: boolean }>(`/menu/categories/${id}`, { method: 'DELETE' }),
  createMenuItem: (item: Partial<MenuItem> & { categoryId: string; name: string; price: number }) => {
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const { menuItemId: _id, ...rest } = item;
    return request<MenuItem>('/menu/items', {
      method: 'POST',
      body: JSON.stringify({ ...rest, isAvailable: rest.isAvailable ?? true, taxRate: rest.taxRate ?? 10 }),
    });
  },
  updateMenuItem: (id: string, item: MenuItem) =>
    request<MenuItem>(`/menu/items/${id}`, { method: 'PUT', body: JSON.stringify(item) }),
  deleteMenuItem: (id: string) =>
    request<{ success: boolean }>(`/menu/items/${id}`, { method: 'DELETE' }),
  toggleMenuItem: (id: string) =>
    request<{ success: boolean }>(`/menu/items/${id}/toggle`, { method: 'POST' }),

  getSettingsTables: () => request<TableSettings[]>('/settings/tables'),
  saveTable: (table: TableSettings) =>
    request<TableSettings>('/settings/tables', { method: 'POST', body: JSON.stringify(table) }),
  deleteTable: (id: string) =>
    request<{ success: boolean }>(`/settings/tables/${id}`, { method: 'DELETE' }),

  getDashboard: () => request<Dashboard>('/dashboard'),
  getSalesReport: (from: string, to: string) =>
    request<SalesReport>(`/dashboard/report?from=${from}&to=${to}`),

  getSession: (id: string) => request<SessionDetail>(`/sessions/${id}`),
  openSession: (tableId: string, guestCount: number) =>
    request<SessionDetail>('/sessions/open', { method: 'POST', body: JSON.stringify({ tableId, guestCount }) }),
  addOrder: (sessionId: string, menuItemId: string, quantity = 1) =>
    request<SessionDetail>(`/sessions/${sessionId}/orders`, { method: 'POST', body: JSON.stringify({ menuItemId, quantity }) }),
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

  getKanban: (section?: string) =>
    request<KanbanOrder[]>(`/orders/kanban${section ? `?section=${encodeURIComponent(section)}` : ''}`),

  updateOrderStatus: (orderItemId: string, status: string, rowVersion: string) =>
    request<{ success: boolean }>(`/orders/${orderItemId}/status`, {
      method: 'POST',
      body: JSON.stringify({ status, rowVersion }),
    }),
};

export function formatCurrency(n: number) {
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(n);
}
