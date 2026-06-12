import { useCallback, useState } from 'react';
import { api } from '../api';
import { usePolling } from '../hooks/usePolling';
import type { KanbanOrder, OrderItemStatus } from '../types';

const columns: { status: OrderItemStatus; label: string; dot: string; color: string }[] = [
  { status: 'Pending',   label: 'Bekliyor',       dot: 'dot-pending',   color: '#f59e0b' },
  { status: 'Preparing', label: 'Hazırlanıyor',   dot: 'dot-preparing', color: '#5b7cfa' },
  { status: 'Served',    label: 'Servis Edildi',  dot: 'dot-served',    color: '#22c55e' },
];

const nextStatus: Partial<Record<OrderItemStatus, OrderItemStatus>> = {
  Pending: 'Preparing',
  Preparing: 'Served',
};

const nextLabel: Partial<Record<OrderItemStatus, string>> = {
  Pending: 'Hazırlamaya Başla →',
  Preparing: 'Servis Edildi ✓',
};

function timeAgo(dateStr: string) {
  const ms = Date.now() - new Date(dateStr).getTime();
  const m = Math.floor(ms / 60000);
  if (m < 1) return 'şimdi';
  if (m < 60) return `${m} dk önce`;
  return `${Math.floor(m / 60)} sa önce`;
}

export default function OrdersPage() {
  const [orders, setOrders] = useState<KanbanOrder[]>([]);
  const [error, setError] = useState('');
  const [advancing, setAdvancing] = useState<string | null>(null);

  const load = useCallback(async () => {
    setOrders(await api.getKanban());
  }, []);

  usePolling(load, 2000, [load]);

  const advance = async (order: KanbanOrder) => {
    const next = nextStatus[order.status];
    if (!next) return;
    setAdvancing(order.orderItemId);
    try {
      await api.updateOrderStatus(order.orderItemId, next, order.rowVersion);
      await load();
    } catch (e) {
      if (e instanceof Error && e.message.includes('409')) {
        // Concurrency conflict — reload and retry
        await load();
      } else {
        setError(e instanceof Error ? e.message : 'Durum güncellenemedi.');
      }
    } finally {
      setAdvancing(null);
    }
  };

  const total = orders.filter((o) => o.status !== 'Served').length;

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 12 }}>
        <div>
          <h1 className="page-title">Mutfak Siparişleri</h1>
          <p className="page-subtitle">
            {total > 0 ? `${total} aktif sipariş` : 'Tüm siparişler tamamlandı'} · canlı güncellenir
          </p>
        </div>
        <button className="btn-secondary" onClick={() => load()} style={{ alignSelf: 'flex-start' }}>
          🔄 Yenile
        </button>
      </div>

      {error && <div className="error-msg" onClick={() => setError('')}>{error}</div>}

      <div className="kanban-grid">
        {columns.map((col) => {
          const colOrders = orders.filter((o) => o.status === col.status);
          return (
            <div key={col.status} className="card" style={{ padding: '18px' }}>
              <div className="kanban-col-header">
                <span className={`kanban-dot ${col.dot}`} />
                <span className="kanban-col-title">{col.label}</span>
                <span className="kanban-count">{colOrders.length}</span>
              </div>

              {colOrders.length === 0 ? (
                <p style={{ color: 'var(--text-muted)', fontSize: 13, padding: '12px 0' }}>
                  {col.status === 'Served' ? 'Henüz servis edilmedi' : 'Bekleyen yok'}
                </p>
              ) : (
                colOrders.map((o) => (
                  <div key={o.orderItemId} className="kanban-card">
                    <div className="kanban-card-name">{o.itemName}</div>
                    <div className="kanban-card-meta">
                      <strong>Masa {o.tableNumber}</strong> · ×{o.quantity}
                    </div>
                    {'orderedAt' in o && (o as KanbanOrder & { orderedAt?: string }).orderedAt && (
                      <div className="kanban-time">
                        ⏱ {timeAgo((o as KanbanOrder & { orderedAt: string }).orderedAt)}
                      </div>
                    )}
                    {nextStatus[o.status] && (
                      <button
                        className="btn-primary"
                        style={{ marginTop: 10, width: '100%', justifyContent: 'center', fontSize: 12, padding: '9px' }}
                        disabled={advancing === o.orderItemId}
                        onClick={() => advance(o)}
                      >
                        {advancing === o.orderItemId ? '…' : nextLabel[o.status]}
                      </button>
                    )}
                  </div>
                ))
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
