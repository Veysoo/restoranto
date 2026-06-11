import { useCallback, useEffect, useState } from 'react';
import { api, formatCurrency } from '../api';
import SessionPanel from '../components/SessionPanel';
import { usePolling } from '../hooks/usePolling';
import type { TableCard, TableStatus } from '../types';

const statusBadge: Record<TableStatus, string> = {
  Empty: 'badge-empty', Occupied: 'badge-occupied', Billed: 'badge-billed', Paid: 'badge-paid',
};
const statusLabel: Record<TableStatus, string> = {
  Empty: 'BOŞTA', Occupied: 'DOLU', Billed: 'HESAP', Paid: 'ÖDENDİ',
};
const statusBarClass: Record<TableStatus, string> = {
  Empty: 'empty', Occupied: 'occupied', Billed: 'billed', Paid: 'paid',
};

export default function FloorPage() {
  const [sections, setSections] = useState<string[]>([]);
  const [selectedSection, setSelectedSection] = useState('Tümü');
  const [tables, setTables] = useState<TableCard[]>([]);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [newSessionTable, setNewSessionTable] = useState<TableCard | null>(null);
  const [guestCount, setGuestCount] = useState(2);
  const [error, setError] = useState('');

  const loadTables = useCallback(async () => {
    const section = selectedSection === 'Tümü' ? undefined : selectedSection;
    const data = await api.getTables(section);
    setTables(data);
  }, [selectedSection]);

  useEffect(() => {
    api.getSections().then((s) => setSections(['Tümü', ...s])).catch(() => {});
  }, []);

  usePolling(loadTables, 2000, [loadTables]);

  const openTable = (table: TableCard) => {
    if (table.status === 'Empty') {
      setNewSessionTable(table);
      setGuestCount(2);
    } else if (table.sessionId) {
      setSessionId(table.sessionId);
    }
  };

  const confirmNewSession = async () => {
    if (!newSessionTable) return;
    try {
      const session = await api.openSession(newSessionTable.tableId, guestCount);
      setNewSessionTable(null);
      await loadTables();
      setSessionId(session.sessionId);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Oturum açılamadı.');
    }
  };

  const occupied = tables.filter((t) => t.status !== 'Empty').length;

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 12 }}>
        <div>
          <h1 className="page-title">Kat Planı</h1>
          <p className="page-subtitle">Canlı masa durumları · {occupied}/{tables.length} dolu</p>
        </div>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          <span className="badge badge-empty" style={{ padding: '6px 12px', fontSize: 12 }}>⬜ Boşta</span>
          <span className="badge badge-occupied" style={{ padding: '6px 12px', fontSize: 12 }}>🟡 Dolu</span>
          <span className="badge badge-billed" style={{ padding: '6px 12px', fontSize: 12 }}>🔴 Hesap</span>
        </div>
      </div>

      {error && <div className="error-msg" onClick={() => setError('')}>{error}</div>}

      <div className="section-pills">
        {sections.map((s) => (
          <span
            key={s}
            className={`pill${selectedSection === s ? ' active' : ''}`}
            onClick={() => setSelectedSection(s)}
          >
            {s}
          </span>
        ))}
      </div>

      <div className="tables-grid">
        {tables.map((table) => (
          <div
            key={table.tableId}
            className={`table-card ${statusBarClass[table.status]}`}
            onClick={() => openTable(table)}
          >
            <div className={`status-bar ${statusBarClass[table.status]}`} />

            <div className="table-top">
              <div>
                <div className="table-num">{table.tableNumber}</div>
                <div className="table-name">{table.name}</div>
              </div>
              <span className={`badge ${statusBadge[table.status]}`}>
                {statusLabel[table.status]}
              </span>
            </div>

            <div className="table-bottom">
              {table.totalAmount > 0 ? (
                <div className="table-amount">{formatCurrency(table.totalAmount)}</div>
              ) : null}
              <div className="table-meta">
                {table.capacity} kişilik
                {table.status !== 'Empty' && table.totalAmount === 0 ? ' · Sipariş bekleniyor' : ''}
              </div>
            </div>
          </div>
        ))}

        {tables.length === 0 && (
          <div className="card" style={{ gridColumn: '1/-1', textAlign: 'center', padding: 48, color: 'var(--text-secondary)' }}>
            <div style={{ fontSize: 32, marginBottom: 12 }}>🏠</div>
            <p>Bu bölümde masa yok. Yönetim ekranından masa ekleyin.</p>
          </div>
        )}
      </div>

      {newSessionTable && (
        <div className="modal" onClick={() => setNewSessionTable(null)}>
          <div className="card modal-box" onClick={(e) => e.stopPropagation()}>
            <div className="modal-title">Yeni Oturum Aç</div>
            <p style={{ color: 'var(--text-secondary)', marginBottom: 20, fontSize: 14 }}>
              {newSessionTable.name} — {newSessionTable.capacity} kişilik
            </p>
            <div className="form-group">
              <label>Misafir Sayısı</label>
              <input
                type="number"
                min={1}
                max={newSessionTable.capacity}
                value={guestCount}
                onChange={(e) => setGuestCount(+e.target.value)}
              />
            </div>
            <div className="modal-actions">
              <button className="btn-ghost" onClick={() => setNewSessionTable(null)}>İptal</button>
              <button className="btn-primary" onClick={confirmNewSession}>Masayı Aç →</button>
            </div>
          </div>
        </div>
      )}

      {sessionId && (
        <SessionPanel
          sessionId={sessionId}
          onClose={() => setSessionId(null)}
          onUpdated={loadTables}
        />
      )}
    </div>
  );
}
