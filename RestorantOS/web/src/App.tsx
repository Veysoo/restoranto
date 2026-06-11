import { Navigate, Route, Routes } from 'react-router-dom';
import { getStoredAuth } from './api';
import Layout from './components/Layout';
import DashboardPage from './pages/DashboardPage';
import FloorPage from './pages/FloorPage';
import LoginPage from './pages/LoginPage';
import OrdersPage from './pages/OrdersPage';
import SettingsPage from './pages/SettingsPage';

function PrivateRoute({ children }: { children: React.ReactNode }) {
  return getStoredAuth() ? <>{children}</> : <Navigate to="/login" replace />;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/" element={<PrivateRoute><Layout /></PrivateRoute>}>
        <Route index element={<Navigate to="/floor" replace />} />
        <Route path="floor" element={<FloorPage />} />
        <Route path="orders" element={<OrdersPage />} />
        <Route path="settings" element={<SettingsPage />} />
        <Route path="dashboard" element={<DashboardPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
