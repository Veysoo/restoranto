import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { clearAuth, getStoredAuth } from '../api';

export default function Layout() {
  const auth = getStoredAuth();
  const navigate = useNavigate();

  const logout = () => {
    clearAuth();
    navigate('/login');
  };

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-logo">
          <div className="sidebar-logo-icon">R</div>
          <div>
            <div style={{ fontWeight: 600 }}>RestaurantOS</div>
            <div style={{ fontSize: 10, color: 'var(--text-secondary)' }}>Web</div>
          </div>
        </div>
        <nav>
          <NavLink to="/floor" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            Kat Planı
          </NavLink>
          <NavLink to="/dashboard" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            Analiz
          </NavLink>
        </nav>
        <div style={{ marginTop: 'auto', padding: '12px 8px' }}>
          <div style={{ fontSize: 13, fontWeight: 600 }}>{auth?.fullName}</div>
          <div style={{ fontSize: 11, color: 'var(--text-secondary)' }}>{auth?.role}</div>
          <button className="btn-ghost" style={{ marginTop: 8 }} onClick={logout}>
            Çıkış
          </button>
        </div>
      </aside>
      <main className="main">
        <Outlet />
      </main>
    </div>
  );
}
