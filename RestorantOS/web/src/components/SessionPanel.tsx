import { useEffect, useState } from 'react';
import { api, formatCurrency } from '../api';
import type { MenuCategory, PaymentMethod, SessionDetail } from '../types';

interface Props {
  sessionId: string;
  onClose: () => void;
  onUpdated: () => void;
}

const statusLabel: Record<string, string> = {
  Pending: 'Bekliyor', Preparing: 'Hazırlanıyor', Served: 'Servis Edildi', Cancelled: 'İptal',
};

export default function SessionPanel({ sessionId, onClose, onUpdated }: Props) {
  const [session, setSession] = useState<SessionDetail | null>(null);
  const [menu, setMenu] = useState<MenuCategory[]>([]);
  const [selectedCat, setSelectedCat] = useState('');
  const [tab, setTab] = useState<'order' | 'payment'>('order');
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('Cash');
  const [cashTendered, setCashTendered] = useState('');
  const [showSuccess, setShowSuccess] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const load = async () => {
    const [s, m] = await Promise.all([api.getSession(sessionId), api.getMenu()]);
    setSession(s);
    setMenu(m);
    if (!selectedCat && m.length) setSelectedCat(m[0].categoryId);
    if (s.status === 'Billed') setTab('payment');
  };

  useEffect(() => { load().catch((e) => setError(e.message)); }, [sessionId]);

  const addOrder = async (menuItemId: string) => {
    if (!session || session.status !== 'Open') return;
    setLoading(true);
    try {
      const s = await api.addOrder(sessionId, menuItemId);
      setSession(s);
      onUpdated();
    } catch (e) { setError(e instanceof Error ? e.message : 'Hata'); }
    finally { setLoading(false); }
  };

  const removeOrder = async (orderItemId: string) => {
    setLoading(true);
    try {
      const s = await api.removeOrder(sessionId, orderItemId);
      setSession(s);
      onUpdated();
    } catch (e) { setError(e instanceof Error ? e.message : 'Hata'); }
    finally { setLoading(false); }
  };

  const requestBill = async () => {
    if (!session?.orderItems.some((i) => i.status !== 'Cancelled')) {
      setError('Önce sipariş ekleyin.');
      return;
    }
    setLoading(true);
    try {
      const s = await api.requestBill(sessionId);
      setSession(s);
      setTab('payment');
      onUpdated();
    } catch (e) { setError(e instanceof Error ? e.message : 'Hata'); }
    finally { setLoading(false); }
  };

  const pay = async () => {
    if (!session) return;
    const due = session.remainingAmount > 0 ? session.remainingAmount : session.finalAmount;
    const cash = parseFloat(cashTendered) || 0;
    if (paymentMethod === 'Cash' && cash > 0 && cash < due) {
      setError('Nakit tutarı yetersiz.');
      return;
    }
    setLoading(true);
    try {
      const s = await api.pay(sessionId, due, paymentMethod, Math.max(0, cash - due));
      if (s.status === 'Paid') {
        setShowSuccess(true);
        onUpdated();
        setTimeout(() => { setShowSuccess(false); onClose(); }, 2200);
      } else {
        setSession(s);
        onUpdated();
      }
    } catch (e) { setError(e instanceof Error ? e.message : 'Hata'); }
    finally { setLoading(false); }
  };

  const cancel = async () => {
    if (!confirm('Oturumu iptal etmek istiyor musunuz?')) return;
    await api.cancelSession(sessionId);
    onUpdated();
    onClose();
  };

  if (!session) return (
    <div className="overlay"><div className="panel"><p>Yükleniyor...</p></div></div>
  );

  const items = menu.find((c) => c.categoryId === selectedCat)?.items.filter((i) => i.isAvailable) ?? [];
  const due = session.remainingAmount > 0 ? session.remainingAmount : session.finalAmount;

  return (
    <>
      <div className="overlay" onClick={onClose}>
        <div className="panel" onClick={(e) => e.stopPropagation()}>
          <div className="panel-header">
            <div>
              <h2 style={{ fontSize: 22 }}>{session.tableName} #{session.tableNumber}</h2>
              <p style={{ color: 'var(--text-secondary)', fontSize: 13 }}>{session.guestCount} misafir</p>
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              <button className="btn-ghost btn-danger" onClick={cancel}>İptal Et</button>
              <button className="btn-ghost" onClick={onClose}>✕</button>
            </div>
          </div>

          {error && <div className="error-msg" onClick={() => setError('')}>{error}</div>}

          <div className="tabs">
            <span className={`pill${tab === 'order' ? ' active' : ''}`} onClick={() => setTab('order')}>Sipariş Girişi</span>
            <span className={`pill${tab === 'payment' ? ' active' : ''}`} onClick={() => session.status !== 'Open' ? setTab('payment') : requestBill()}>Hesap & Ödeme</span>
          </div>

          {tab === 'order' && (
            <>
              <p style={{ fontSize: 11, fontWeight: 700, color: 'var(--text-secondary)', marginBottom: 8 }}>MENÜ</p>
              <div style={{ marginBottom: 12 }}>
                {menu.map((c) => (
                  <span key={c.categoryId} className={`pill${selectedCat === c.categoryId ? ' active' : ''}`}
                    onClick={() => setSelectedCat(c.categoryId)}>{c.name}</span>
                ))}
              </div>
              <div className="menu-grid">
                {items.map((item) => (
                  <button key={item.menuItemId} className="menu-btn" disabled={loading || session.status !== 'Open'}
                    onClick={() => addOrder(item.menuItemId)}>
                    <div>{item.name}</div>
                    <div className="price">{formatCurrency(item.price)}</div>
                  </button>
                ))}
              </div>
              <p style={{ fontSize: 11, fontWeight: 700, color: 'var(--text-secondary)', marginBottom: 8 }}>SİPARİŞ LİSTESİ</p>
              {session.orderItems.map((item) => (
                <div key={item.orderItemId} className="order-row">
                  <div>
                    <div style={{ fontWeight: 600 }}>{item.name}</div>
                    <div style={{ fontSize: 12, color: 'var(--text-secondary)' }}>{statusLabel[item.status]}</div>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                    <span>x{item.quantity}</span>
                    <span className="mono">{formatCurrency(item.lineTotal)}</span>
                    {session.status === 'Open' && (
                      <button className="btn-ghost" onClick={() => removeOrder(item.orderItemId)}>✕</button>
                    )}
                  </div>
                </div>
              ))}
              <div className="card" style={{ marginTop: 12 }}>
                <div className="summary-row"><span>Ara Toplam</span><span className="mono">{formatCurrency(session.totalAmount)}</span></div>
              </div>
              <button className="btn-primary" style={{ width: '100%', marginTop: 16 }} onClick={requestBill} disabled={loading}>
                Hesap Kes →
              </button>
            </>
          )}

          {tab === 'payment' && (
            <>
              <div className="card" style={{ marginBottom: 16 }}>
                <p style={{ fontSize: 11, fontWeight: 700, color: 'var(--text-secondary)', marginBottom: 12 }}>HESAP ÖZETİ</p>
                <div className="summary-row"><span>Ara Toplam</span><span className="mono">{formatCurrency(session.totalAmount)}</span></div>
                <div className="summary-row"><span>İndirim</span><span className="mono">{formatCurrency(session.discountAmount)}</span></div>
                <div className="summary-row"><span>KDV</span><span className="mono">{formatCurrency(session.taxAmount)}</span></div>
                <hr style={{ border: 'none', borderTop: '1px solid var(--border)', margin: '12px 0' }} />
                <div className="summary-row"><span style={{ fontWeight: 700, color: 'var(--text-primary)' }}>TOPLAM</span>
                  <span className="summary-total">{formatCurrency(session.finalAmount)}</span></div>
                <div className="summary-row"><span>Ödenen</span><span className="mono" style={{ color: 'var(--accent-success)' }}>{formatCurrency(session.paidAmount)}</span></div>
                <div className="summary-row"><span>Kalan</span><span className="mono" style={{ color: 'var(--accent-warning)' }}>{formatCurrency(session.remainingAmount)}</span></div>
              </div>
              <p style={{ fontSize: 11, fontWeight: 700, color: 'var(--text-secondary)', marginBottom: 8 }}>ÖDEME YÖNTEMİ</p>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginBottom: 16 }}>
                {(['Cash', 'CreditCard', 'DebitCard', 'Transfer'] as PaymentMethod[]).map((m) => (
                  <button key={m} className={`btn-secondary${paymentMethod === m ? ' active' : ''}`}
                    style={paymentMethod === m ? { borderColor: 'var(--accent)' } : {}}
                    onClick={() => setPaymentMethod(m)}>
                    {m === 'Cash' ? 'Nakit' : m === 'CreditCard' ? 'Kredi Kartı' : m === 'DebitCard' ? 'Banka Kartı' : 'Havale'}
                  </button>
                ))}
              </div>
              {paymentMethod === 'Cash' && (
                <div className="form-group">
                  <label>NAKİT VERİLEN</label>
                  <input type="number" value={cashTendered} onChange={(e) => setCashTendered(e.target.value)} />
                </div>
              )}
              <button className="btn-primary" style={{ width: '100%' }} onClick={pay} disabled={loading}>
                Ödemeyi Kaydet
              </button>
            </>
          )}
        </div>
      </div>

      {showSuccess && (
        <div className="success-overlay">
          <div className="card success-box">
            <div className="check">✓</div>
            <h2 style={{ marginTop: 12 }}>Ödeme Tamamlandı!</h2>
            <p style={{ color: 'var(--text-secondary)', marginTop: 8 }}>{formatCurrency(due)} kaydedildi.</p>
          </div>
        </div>
      )}
    </>
  );
}
