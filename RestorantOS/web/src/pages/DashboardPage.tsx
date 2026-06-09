import { useEffect, useState } from 'react';
import { api, formatCurrency } from '../api';
import type { Dashboard } from '../types';

export default function DashboardPage() {
  const [data, setData] = useState<Dashboard | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api.getDashboard().then(setData).catch((e) => setError(e.message));
    const t = setInterval(() => api.getDashboard().then(setData).catch(() => {}), 10000);
    return () => clearInterval(t);
  }, []);

  if (error) return <div className="error-msg">{error}</div>;
  if (!data) return <p style={{ color: 'var(--text-secondary)' }}>Yükleniyor...</p>;

  return (
    <div>
      <h1 className="page-title">Analiz</h1>
      <p className="page-subtitle">Günlük performans ve satış özeti</p>

      <div className="stats-row">
        <div className="card stat-card">
          <h3>Bugünkü Gelir</h3>
          <div className="value" style={{ color: 'var(--accent-success)' }}>{formatCurrency(data.todayRevenue)}</div>
        </div>
        <div className="card stat-card">
          <h3>Açık Masalar</h3>
          <div className="value">{data.openTables} / {data.totalTables}</div>
        </div>
        <div className="card stat-card">
          <h3>Aktif Sipariş</h3>
          <div className="value">{data.activeOrdersCount}</div>
        </div>
        <div className="card stat-card">
          <h3>Ortalama Hesap</h3>
          <div className="value">{formatCurrency(data.averageTicketValue)}</div>
        </div>
      </div>

      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
          <div>
            <h2 style={{ fontSize: 18 }}>Bugün Satılanlar</h2>
            <p style={{ color: 'var(--text-secondary)', fontSize: 13 }}>Ödeme tamamlanan ürünler</p>
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            <span className="pill">{data.todaySessionCount} oturum</span>
            <span className="pill">{data.todayItemsSold} ürün</span>
          </div>
        </div>
        {data.todaySoldItems.length === 0 ? (
          <p style={{ color: 'var(--text-secondary)' }}>Henüz bugün satış yok.</p>
        ) : (
          <table className="data-table">
            <thead>
              <tr><th>ÜRÜN</th><th>ADET</th><th>GELİR</th></tr>
            </thead>
            <tbody>
              {data.todaySoldItems.map((item) => (
                <tr key={item.itemName}>
                  <td>{item.itemName}</td>
                  <td>{item.quantity}</td>
                  <td className="mono">{formatCurrency(item.revenue)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
