import { useCallback, useEffect, useRef, useState } from 'react';
import { api, formatCurrency } from '../api';
import type { MenuCategory, PaymentMethod, SessionDetail } from '../types';

interface Props {
  sessionId: string;
  onClose: () => void;
  onUpdated: () => void;
}

const orderStatusLabel: Record<string, string> = {
  Pending: 'Bekliyor', Preparing: 'Hazırlanıyor', Served: 'Servis Edildi', Cancelled: 'İptal',
};
const orderStatusClass: Record<string, string> = {
  Pending: 'status-pending', Preparing: 'status-preparing', Served: 'status-served', Cancelled: 'status-cancelled',
};

const payMethods: { value: PaymentMethod; label: string; icon: string }[] = [
  { value: 'Cash',       label: 'Nakit',      icon: '💵' },
  { value: 'CreditCard', label: 'Kredi Kartı', icon: '💳' },
  { value: 'DebitCard',  label: 'Banka Kartı', icon: '🏦' },
  { value: 'Transfer',   label: 'Havale',      icon: '📲' },
];

export default function SessionPanel({ sessionId, onClose, onUpdated }: Props) {
  const [session, setSession] = useState<SessionDetail | null>(null);
  const [menu, setMenu] = useState<MenuCategory[]>([]);
  const [selectedCat, setSelectedCat] = useState('');
  const [tab, setTab] = useState<'order' | 'payment'>('order');
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('Cash');
  const [cashTendered, setCashTendered] = useState('');
  const [showSuccess, setShowSuccess] = useState(false);
  const [paidAmount, setPaidAmount] = useState(0);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const menuLoaded = useRef(false);

  const loadSession = useCallback(async () => {
    const s = await api.getSession(sessionId);
    setSession(s);
    if (s.status === 'Billed' || s.status === 'Paid') setTab('payment');
  }, [sessionId]);

  const loadMenu = useCallback(async () => {
    if (menuLoaded.current) return;
    const m = await api.getMenu();
    setMenu(m);
    if (m.length) setSelectedCat((prev) => prev || m[0].categoryId);
    menuLoaded.current = true;
  }, []);

  // Initial load
  useEffect(() => {
    Promise.all([loadSession(), loadMenu()]).catch((e) => setError(e instanceof Error ? e.message : 'Yüklenemedi.'));
  }, [sessionId]);

  // Poll session for real-time updates (2s while panel is open)
  useEffect(() => {
    const interval = setInterval(() => {
      if (!document.hidden) loadSession().catch(() => {});
    }, 2000);
    return () => clearInterval(interval);
  }, [loadSession]);

  const addOrder = async (menuItemId: string) => {
    if (!session || session.status !== 'Open') return;
    setLoading(true);
    setError('');
    try {
      const s = await api.addOrder(sessionId, menuItemId);
      setSession(s);
      onUpdated();
    } catch (e) { setError(e instanceof Error ? e.message : 'Sipariş eklenemedi.'); }
    finally { setLoading(false); }
  };

  const removeOrder = async (orderItemId: string) => {
    setLoading(true);
    setError('');
    try {
      const s = await api.removeOrder(sessionId, orderItemId);
      setSession(s);
      onUpdated();
    } catch (e) { setError(e instanceof Error ? e.message : 'Sipariş kaldırılamadı.'); }
    finally { setLoading(false); }
  };

  const requestBill = async () => {
    const activeItems = session?.orderItems.filter((i) => i.status !== 'Cancelled') ?? [];
    if (activeItems.length === 0) { setError('Önce sipariş ekleyin.'); return; }
    setLoading(true);
    setError('');
    try {
      const s = await api.requestBill(sessionId);
      setSession(s);
      setTab('payment');
      onUpdated();
    } catch (e) { setError(e instanceof Error ? e.message : 'Hesap kesilemedi.'); }
    finally { setLoading(false); }
  };

  const pay = async () => {
    if (!session) return;
    const due = session.remainingAmount > 0 ? session.remainingAmount : session.finalAmount;
    const cash = parseFloat(cashTendered) || 0;
    if (paymentMethod === 'Cash' && cashTendered && cash < due) {
      setError(`Nakit tutar yetersiz. En az ${formatCurrency(due)} girilmeli.`);
      return;
    }
    setLoading(true);
    try {
      const change = paymentMethod === 'Cash' && cash > due ? cash - due : 0;
      const s = await api.pay(sessionId, due, paymentMethod, change);
      if (s.status === 'Paid') {
        setPaidAmount(due);
        setShowSuccess(true);
        onUpdated();
        setTimeout(() => { setShowSuccess(false); onClose(); }, 2500);
      } else {
        setSession(s);
        onUpdated();
      }
    } catch (e) { setError(e instanceof Error ? e.message : 'Ödeme kaydedilemedi.'); }
    finally { setLoading(false); }
  };

  const cancel = async () => {
    if (!confirm('Bu masanın oturumunu iptal etmek istiyor musunuz? Tüm siparişler silinecek.')) return;
    try {
      await api.cancelSession(sessionId);
      onUpdated();
      onClose();
    } catch (e) { setError(e instanceof Error ? e.message : 'İptal edilemedi.'); }
  };

  if (!session) return (
    <div className="overlay">
      <div className="panel" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <p style={{ color: 'var(--text-secondary)' }}>Yükleniyor…</p>
      </div>
    </div>
  );

  const activeItems = menu.find((c) => c.categoryId === selectedCat)?.items.filter((i) => i.isAvailable) ?? [];
  const due = session.remainingAmount > 0 ? session.remainingAmount : session.finalAmount;
  const cash = parseFloat(cashTendered) || 0;
  const change = paymentMethod === 'Cash' && cash > due ? cash - due : 0;

  return (
    <>
      <div className="overlay" onClick={onClose}>
        <div className="panel" onClick={(e) => e.stopPropagation()}>
          {/* Header */}
          <div className="panel-header">
            <div>
              <h2 style={{ fontSize: 20, fontWeight: 700 }}>
                {session.tableName} — Masa {session.tableNumber}
              </h2>
              <p style={{ color: 'var(--text-secondary)', fontSize: 13, marginTop: 3 }}>
                {session.guestCount} misafir
                {session.totalAmount > 0 && ` · ${formatCurrency(session.totalAmount)}`}
              </p>
            </div>
            <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
              {session.status === 'Open' && (
                <button className="btn-ghost btn-danger" onClick={cancel}>🗑️ İptal</button>
              )}
              <button className="btn-ghost" onClick={onClose} style={{ fontSize: 18 }}>✕</button>
            </div>
          </div>

          <div className="panel-body">
            {error && <div className="error-msg" onClick={() => setError('')}>{error}</div>}

            {/* Tabs */}
            <div className="tabs">
              <span className={`pill${tab === 'order' ? ' active' : ''}`} onClick={() => setTab('order')}>
                🛒 Sipariş
              </span>
              <span
                className={`pill${tab === 'payment' ? ' active' : ''}`}
                onClick={async () => {
                  if (session.status === 'Open') await requestBill();
                  else setTab('payment');
                }}
              >
                💰 Hesap & Ödeme
              </span>
            </div>

            {/* --- ORDER TAB --- */}
            {tab === 'order' && (
              <>
                <div className="section-label">Menü Kategorisi</div>
                <div style={{ marginBottom: 16 }}>
                  {menu.map((c) => (
                    <span
                      key={c.categoryId}
                      className={`pill${selectedCat === c.categoryId ? ' active' : ''}`}
                      onClick={() => setSelectedCat(c.categoryId)}
                    >
                      {c.icon} {c.name}
                    </span>
                  ))}
                </div>

                <div className="menu-grid">
                  {activeItems.map((item) => (
                    <button
                      key={item.menuItemId}
                      className="menu-btn"
                      disabled={loading || session.status !== 'Open'}
                      onClick={() => addOrder(item.menuItemId)}
                    >
                      <div style={{ fontWeight: 600 }}>{item.name}</div>
                      <div className="price">{formatCurrency(item.price)}</div>
                      {item.prepTimeMinutes ? (
                        <div className="prep">⏱ {item.prepTimeMinutes} dk</div>
                      ) : null}
                    </button>
                  ))}
                  {activeItems.length === 0 && (
                    <p style={{ color: 'var(--text-muted)', fontSize: 13, gridColumn: '1/-1' }}>
                      Bu kategoride ürün yok.
                    </p>
                  )}
                </div>

                <div className="section-label">Sipariş Listesi</div>

                {session.orderItems.length === 0 ? (
                  <p style={{ color: 'var(--text-muted)', fontSize: 13, marginBottom: 16 }}>
                    Henüz sipariş eklenmedi. Yukarıdan ürün seçin.
                  </p>
                ) : (
                  session.orderItems.map((item) => (
                    <div key={item.orderItemId} className="order-row">
                      <div>
                        <div style={{ fontWeight: 600, fontSize: 14 }}>{item.name}</div>
                        <span className={`order-item-status ${orderStatusClass[item.status]}`}>
                          {orderStatusLabel[item.status]}
                        </span>
                      </div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                        <span style={{ color: 'var(--text-secondary)', fontSize: 13 }}>×{item.quantity}</span>
                        <span className="mono" style={{ fontWeight: 600 }}>{formatCurrency(item.lineTotal)}</span>
                        {session.status === 'Open' && (
                          <button className="btn-ghost btn-danger" onClick={() => removeOrder(item.orderItemId)} style={{ padding: '4px 8px' }}>✕</button>
                        )}
                      </div>
                    </div>
                  ))
                )}

                <div className="summary-card">
                  <div className="summary-row">
                    <span>Ara Toplam</span>
                    <span className="mono">{formatCurrency(session.totalAmount)}</span>
                  </div>
                </div>

                {session.status === 'Open' && (
                  <button
                    className="btn-primary"
                    style={{ width: '100%', marginTop: 16, justifyContent: 'center' }}
                    onClick={requestBill}
                    disabled={loading}
                  >
                    Hesap Kes →
                  </button>
                )}
              </>
            )}

            {/* --- PAYMENT TAB --- */}
            {tab === 'payment' && (
              <>
                <div className="summary-card" style={{ marginBottom: 20 }}>
                  <div className="section-label" style={{ marginBottom: 12 }}>Hesap Özeti</div>
                  <div className="summary-row"><span>Ara Toplam</span><span className="mono">{formatCurrency(session.totalAmount)}</span></div>
                  {session.discountAmount > 0 && (
                    <div className="summary-row"><span>İndirim</span><span className="mono" style={{ color: 'var(--accent-success)' }}>-{formatCurrency(session.discountAmount)}</span></div>
                  )}
                  <div className="summary-row"><span>KDV (%{((session.taxAmount / Math.max(session.totalAmount, 1)) * 100).toFixed(0)})</span><span className="mono">{formatCurrency(session.taxAmount)}</span></div>
                  <div className="summary-row total">
                    <span>TOPLAM</span>
                    <span className="summary-total">{formatCurrency(session.finalAmount)}</span>
                  </div>
                  {session.paidAmount > 0 && (
                    <>
                      <div className="summary-row"><span>Ödenen</span><span className="mono" style={{ color: 'var(--accent-success)' }}>{formatCurrency(session.paidAmount)}</span></div>
                      <div className="summary-row"><span>Kalan</span><span className="mono" style={{ color: 'var(--accent-warning)' }}>{formatCurrency(session.remainingAmount)}</span></div>
                    </>
                  )}
                </div>

                <div className="section-label">Ödeme Yöntemi</div>
                <div className="payment-methods">
                  {payMethods.map((m) => (
                    <button
                      key={m.value}
                      className={`pay-method-btn${paymentMethod === m.value ? ' selected' : ''}`}
                      onClick={() => setPaymentMethod(m.value)}
                    >
                      <span className="pay-icon">{m.icon}</span>
                      {m.label}
                    </button>
                  ))}
                </div>

                {paymentMethod === 'Cash' && (
                  <div className="form-group">
                    <label>Verilen Nakit (₺)</label>
                    <input
                      type="number"
                      step="0.01"
                      min={due}
                      value={cashTendered}
                      onChange={(e) => setCashTendered(e.target.value)}
                      placeholder={`En az ${due.toFixed(2)}`}
                    />
                    {change > 0 && (
                      <p style={{ marginTop: 6, color: 'var(--accent-success)', fontSize: 13, fontWeight: 600 }}>
                        Para Üstü: {formatCurrency(change)}
                      </p>
                    )}
                  </div>
                )}

                <button
                  className="btn-primary"
                  style={{ width: '100%', marginTop: 8, justifyContent: 'center', fontSize: 15, padding: '14px' }}
                  onClick={pay}
                  disabled={loading}
                >
                  {loading ? 'İşleniyor…' : `${formatCurrency(due)} Ödemeyi Tamamla ✓`}
                </button>
              </>
            )}
          </div>
        </div>
      </div>

      {showSuccess && (
        <div className="success-overlay">
          <div className="card success-box">
            <div className="success-check">✓</div>
            <h2 style={{ marginTop: 4, fontSize: 22 }}>Ödeme Tamamlandı!</h2>
            <div className="success-amount">{formatCurrency(paidAmount)}</div>
            <p style={{ color: 'var(--text-secondary)', marginTop: 8, fontSize: 14 }}>Masa kapatılıyor…</p>
          </div>
        </div>
      )}
    </>
  );
}
