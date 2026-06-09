import { useCallback, useEffect, useState } from 'react';
import { api, formatCurrency } from '../api';
import SessionPanel from '../components/SessionPanel';
import type { TableCard, TableStatus } from '../types';

const statusText: Record<TableStatus, string> = {
  Empty: 'BOŞTA', Occupied: 'DOLU', Billed: 'HESAP İSTEDİ', Paid: 'ÖDENDİ',
};

const statusClass: Record<TableStatus, string> = {
  Empty: 'status-empty', Occupied: 'status-occupied', Billed: 'status-billed', Paid: 'status-paid',
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

  useEffect(() => {
    loadTables().catch((e) => setError(e.message));
    const t = setInterval(() => loadTables().catch(() => {}), 5000);
    return () => clearInterval(t);
  }, [loadTables]);

  const openTable = (table: TableCard) => {
    if (table.status === 'Empty') {
      setNewSessionTable(table);
      setGuestCount(2);
      return;
    }
    if (table.sessionId) setSessionId(table.sessionId);
  };

  const confirmNewSession = async () => {
    if (!newSessionTable) return;
    try {
      const session = await api.openSession(newSessionTable.tableId, guestCount);
      setNewSessionTable(null);
      await loadTables();
      setSessionId(session.sessionId);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Hata');
    }
  };

  return (
    <div>
      <h1 className="page-title">Kat Planı</h1>
      <p className="page-subtitle">Masa durumlarını canlı takip edin — ağdaki tüm cihazlar senkron</p>

      {error && <div className="error-msg">{error}</div>}

      <div style={{ marginBottom: 20 }}>
        {sections.map((s) => (
          <span key={s} className={`pill${selectedSection === s ? ' active' : ''}`}
            onClick={() => setSelectedSection(s)}>{s}</span>
        ))}
      </div>

      <div className="tables-grid">
        {tables.map((table) => (
          <div key={table.tableId} className="table-card" onClick={() => openTable(table)}>
            <div className={`table-card-bar ${statusClass[table.status]}`} />
            <span className={`table-badge ${statusClass[table.status]}`}>{statusText[table.status]}</span>
            <div className="table-number">{table.tableNumber}</div>
            <div className="table-name">{table.name}</div>
            <div style={{ marginTop: 12, fontSize: 12, color: 'var(--text-secondary)' }}>
              Kapasite {table.capacity}
            </div>
            {table.totalAmount > 0 && (
              <div className="mono" style={{ marginTop: 8, fontSize: 18, fontWeight: 600 }}>
                {formatCurrency(table.totalAmount)}
              </div>
            )}
          </div>
        ))}
      </div>

      {newSessionTable && (
        <div className="modal">
          <div className="card modal-box">
            <h2 style={{ marginBottom: 8 }}>Yeni Oturum</h2>
            <p style={{ color: 'var(--text-secondary)', marginBottom: 20 }}>{newSessionTable.name}</p>
            <div className="form-group">
              <label>MİSAFİR SAYISI</label>
              <input type="number" min={1} value={guestCount} onChange={(e) => setGuestCount(+e.target.value)} />
            </div>
            <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end', marginTop: 20 }}>
              <button className="btn-secondary" onClick={() => setNewSessionTable(null)}>İptal</button>
              <button className="btn-primary" onClick={confirmNewSession}>Aç</button>
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
