import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { clearAuth, getStoredAuth } from '../api';

const navItems = [
  { to: '/floor',     label: 'Kat Planı',   icon: '🏠' },
  { to: '/orders',   label: 'Siparişler',  icon: '🍽️' },
  { to: '/settings', label: 'Yönetim',     icon: '⚙️' },
  { to: '/dashboard',label: 'Analiz',      icon: '📊' },
];

export default function Layout() {
  const auth = getStoredAuth();
  const navigate = useNavigate();

  const logout = () => { clearAuth(); navigate('/login'); };

  const initials = auth?.fullName
    ? auth.fullName.split(' ').map((w) => w[0]).slice(0, 2).join('').toUpperCase()
    : '?';

  const roleLabel: Record<string, string> = { Admin: 'Yönetici', Waiter: 'Garson', Cashier: 'Kasiyer' };

  return (
    <div className="app-shell">
      <aside className="sidebar desktop-only">
        <div className="sidebar-logo">
          <div className="sidebar-logo-icon">R</div>
          <div>
            <div className="sidebar-brand">RestaurantOS</div>
            <div className="sidebar-brand-sub">v2.0 · Restoran Yönetimi</div>
          </div>
        </div>

        <nav className="sidebar-nav">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
            >
              <span className="nav-link-icon">{item.icon}</span>
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="sidebar-avatar">{initials}</div>
          <div>
            <div className="sidebar-user-name">{auth?.fullName ?? 'Kullanıcı'}</div>
            <div className="sidebar-user-role">{roleLabel[auth?.role ?? ''] ?? auth?.role}</div>
          </div>
          <button className="logout-btn" onClick={logout} title="Çıkış Yap">⏻</button>
        </div>
      </aside>

      <main className="main">
        <Outlet />
      </main>

      <nav className="bottom-nav mobile-only">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) => `bottom-nav-item${isActive ? ' active' : ''}`}
          >
            <span className="bottom-nav-icon">{item.icon}</span>
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>
    </div>
  );
}
