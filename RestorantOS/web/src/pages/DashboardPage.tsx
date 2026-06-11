import { useCallback, useRef, useState } from 'react';
import { api, formatCurrency } from '../api';
import { usePolling } from '../hooks/usePolling';
import type { Dashboard, SalesReport } from '../types';

function toLocalDateStr(date: Date) {
  return date.toISOString().slice(0, 10);
}

function fmtDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' });
}

function fmtLongDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('tr-TR', { weekday: 'short', day: 'numeric', month: 'long', year: 'numeric' });
}

export default function DashboardPage() {
  const [data, setData] = useState<Dashboard | null>(null);
  const [activeTab, setActiveTab] = useState<'today' | 'report'>('today');

  // Report state
  const [reportFrom, setReportFrom] = useState(() => toLocalDateStr(new Date(Date.now() - 6 * 86400000)));
  const [reportTo, setReportTo] = useState(() => toLocalDateStr(new Date()));
  const [report, setReport] = useState<SalesReport | null>(null);
  const [reportLoading, setReportLoading] = useState(false);
  const [reportError, setReportError] = useState('');
  const printRef = useRef<HTMLDivElement>(null);

  const load = useCallback(async () => {
    setData(await api.getDashboard());
  }, []);

  usePolling(load, 5000, [load]);

  const loadReport = async () => {
    setReportLoading(true);
    setReportError('');
    try {
      const r = await api.getSalesReport(reportFrom, reportTo);
      setReport(r);
    } catch (e) {
      setReportError(e instanceof Error ? e.message : 'Rapor yüklenemedi.');
    } finally {
      setReportLoading(false);
    }
  };

  const printPdf = () => {
    const el = printRef.current;
    if (!el) return;
    const html = `<!DOCTYPE html><html lang="tr"><head><meta charset="utf-8">
      <title>Satış Raporu ${reportFrom} — ${reportTo}</title>
      <style>
        body{font-family:sans-serif;color:#111;padding:24px;font-size:13px}
        h1{font-size:20px;margin-bottom:4px}
        .subtitle{color:#666;margin-bottom:24px;font-size:12px}
        .stats{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin-bottom:24px}
        .stat{border:1px solid #e5e7eb;border-radius:8px;padding:14px}
        .stat-label{font-size:11px;color:#888;margin-bottom:4px;text-transform:uppercase}
        .stat-val{font-size:20px;font-weight:700}
        table{width:100%;border-collapse:collapse;margin-bottom:24px}
        th{background:#f9fafb;text-align:left;padding:8px 10px;font-size:11px;color:#666;text-transform:uppercase;border-bottom:2px solid #e5e7eb}
        td{padding:7px 10px;border-bottom:1px solid #f3f4f6;font-size:12px}
        .money{font-weight:600;color:#16a34a}
        @media print{body{padding:0}}
      </style></head><body>${el.innerHTML}</body></html>`;
    const w = window.open('', '_blank', 'width=900,height=700');
    if (w) {
      w.document.write(html);
      w.document.close();
      setTimeout(() => { w.focus(); w.print(); }, 400);
    }
  };

  const occupancyPct = data && data.totalTables > 0
    ? Math.round((data.openTables / data.totalTables) * 100) : 0;

  const maxRev = report ? Math.max(...report.dailyBreakdown.map((d) => d.revenue), 1) : 1;

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 12 }}>
        <div>
          <h1 className="page-title">Analiz</h1>
          <p className="page-subtitle">Satış ve performans raporları</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <span
            className={`pill${activeTab === 'today' ? ' active' : ''}`}
            onClick={() => setActiveTab('today')}
          >
            📊 Bugün
          </span>
          <span
            className={`pill${activeTab === 'report' ? ' active' : ''}`}
            onClick={() => { setActiveTab('report'); if (!report) loadReport(); }}
          >
            📅 Tarih Aralığı
          </span>
        </div>
      </div>

      {/* ── TODAY TAB ── */}
      {activeTab === 'today' && data && (
        <>
          <div className="stats-grid">
            <div className="card stat-card revenue">
              <span className="stat-icon">💰</span>
              <div className="stat-label">Bugünkü Gelir</div>
              <div className="stat-value" style={{ color: 'var(--accent-success)' }}>
                {formatCurrency(data.todayRevenue)}
              </div>
              <div className="stat-sub">{data.todaySessionCount} tamamlanan oturum</div>
            </div>
            <div className="card stat-card tables">
              <span className="stat-icon">🏠</span>
              <div className="stat-label">Açık Masalar</div>
              <div className="stat-value">{data.openTables} / {data.totalTables}</div>
              <div className="stat-sub">%{occupancyPct} doluluk</div>
            </div>
            <div className="card stat-card orders">
              <span className="stat-icon">🍽️</span>
              <div className="stat-label">Aktif Sipariş</div>
              <div className="stat-value">{data.activeOrdersCount}</div>
              <div className="stat-sub">{data.todayItemsSold} ürün bugün</div>
            </div>
            <div className="card stat-card ticket">
              <span className="stat-icon">📈</span>
              <div className="stat-label">Ortalama Hesap</div>
              <div className="stat-value">{formatCurrency(data.averageTicketValue)}</div>
              <div className="stat-sub">Bugünkü ortalama</div>
            </div>
          </div>

          <div className="card">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 18, flexWrap: 'wrap', gap: 10 }}>
              <div>
                <h2 style={{ fontSize: 17, fontWeight: 600 }}>Bugün Satılanlar</h2>
                <p style={{ color: 'var(--text-secondary)', fontSize: 13, marginTop: 3 }}>Ödeme tamamlanan oturumlardan</p>
              </div>
              <div style={{ display: 'flex', gap: 8 }}>
                <span className="pill" style={{ cursor: 'default' }}>{data.todaySessionCount} oturum</span>
                <span className="pill" style={{ cursor: 'default' }}>{data.todayItemsSold} ürün</span>
              </div>
            </div>
            {data.todaySoldItems.length === 0 ? (
              <div style={{ textAlign: 'center', padding: '32px 0', color: 'var(--text-secondary)' }}>
                <div style={{ fontSize: 32, marginBottom: 10 }}>📭</div>
                <p>Bugün henüz satış yok.</p>
              </div>
            ) : (
              <table className="data-table">
                <thead>
                  <tr><th>#</th><th>ÜRÜN ADI</th><th>ADET</th><th>GELİR</th></tr>
                </thead>
                <tbody>
                  {data.todaySoldItems.map((item, i) => (
                    <tr key={item.itemName}>
                      <td><span className={`rank-badge${i < 3 ? ' top' : ''}`}>{i + 1}</span></td>
                      <td style={{ fontWeight: 500 }}>{item.itemName}</td>
                      <td>{item.quantity}</td>
                      <td className="mono" style={{ fontWeight: 600, color: 'var(--accent-success)' }}>
                        {formatCurrency(item.revenue)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </>
      )}

      {/* ── REPORT TAB ── */}
      {activeTab === 'report' && (
        <>
          {/* Date picker */}
          <div className="card" style={{ marginBottom: 16 }}>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'flex-end' }}>
              <div className="form-group" style={{ marginBottom: 0 }}>
                <label>Başlangıç Tarihi</label>
                <input
                  type="date"
                  value={reportFrom}
                  max={reportTo}
                  onChange={(e) => setReportFrom(e.target.value)}
                />
              </div>
              <div className="form-group" style={{ marginBottom: 0 }}>
                <label>Bitiş Tarihi</label>
                <input
                  type="date"
                  value={reportTo}
                  min={reportFrom}
                  max={toLocalDateStr(new Date())}
                  onChange={(e) => setReportTo(e.target.value)}
                />
              </div>
              <div style={{ display: 'flex', gap: 8 }}>
                {/* Quick presets */}
                {[
                  { label: 'Son 7 gün', days: 7 },
                  { label: 'Son 30 gün', days: 30 },
                  { label: 'Bu ay', days: new Date().getDate() },
                ].map((p) => (
                  <button
                    key={p.label}
                    className="btn-secondary"
                    onClick={() => {
                      const t = new Date(); const f = new Date(t.getTime() - (p.days - 1) * 86400000);
                      setReportFrom(toLocalDateStr(f)); setReportTo(toLocalDateStr(t));
                    }}
                  >{p.label}</button>
                ))}
                <button className="btn-primary" onClick={loadReport} disabled={reportLoading}>
                  {reportLoading ? '⟳ Yükleniyor…' : '🔍 Raporla'}
                </button>
              </div>
            </div>
          </div>

          {reportError && <div className="error-msg" onClick={() => setReportError('')}>{reportError}</div>}

          {report && (
            <>
              {/* Summary stats */}
              <div className="stats-grid" style={{ marginBottom: 16 }}>
                <div className="card stat-card revenue">
                  <span className="stat-icon">💰</span>
                  <div className="stat-label">Toplam Gelir</div>
                  <div className="stat-value" style={{ color: 'var(--accent-success)' }}>{formatCurrency(report.totalRevenue)}</div>
                  <div className="stat-sub">{fmtDate(report.from)} – {fmtDate(report.to)}</div>
                </div>
                <div className="card stat-card tables">
                  <span className="stat-icon">🧾</span>
                  <div className="stat-label">Toplam Oturum</div>
                  <div className="stat-value">{report.totalSessions}</div>
                  <div className="stat-sub">Ödeme tamamlanan</div>
                </div>
                <div className="card stat-card orders">
                  <span className="stat-icon">🍽️</span>
                  <div className="stat-label">Satılan Ürün</div>
                  <div className="stat-value">{report.totalItemsSold}</div>
                  <div className="stat-sub">Toplam adet</div>
                </div>
                <div className="card stat-card ticket">
                  <span className="stat-icon">📈</span>
                  <div className="stat-label">Ortalama Hesap</div>
                  <div className="stat-value">{formatCurrency(report.averageTicket)}</div>
                  <div className="stat-sub">Oturum başına</div>
                </div>
              </div>

              {/* PDF export button */}
              <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 12 }}>
                <button className="btn-secondary" onClick={printPdf} style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                  🖨️ PDF Çıktı Al
                </button>
              </div>

              {/* Printable content */}
              <div ref={printRef}>
                {/* PDF Header (only visible in print) */}
                <div className="pdf-header" style={{ display: 'none' }}>
                  <h1>RestorantOS — Satış Raporu</h1>
                  <p className="pdf-subtitle">{fmtLongDate(report.from)} – {fmtLongDate(report.to)}</p>
                  <div className="pdf-stats">
                    <div className="pdf-stat"><div className="pdf-stat-label">Toplam Gelir</div><div className="pdf-stat-val">{formatCurrency(report.totalRevenue)}</div></div>
                    <div className="pdf-stat"><div className="pdf-stat-label">Toplam Oturum</div><div className="pdf-stat-val">{report.totalSessions}</div></div>
                    <div className="pdf-stat"><div className="pdf-stat-label">Satılan Ürün</div><div className="pdf-stat-val">{report.totalItemsSold}</div></div>
                    <div className="pdf-stat"><div className="pdf-stat-label">Ort. Hesap</div><div className="pdf-stat-val">{formatCurrency(report.averageTicket)}</div></div>
                  </div>
                </div>

                {/* Daily bar chart */}
                <div className="card" style={{ marginBottom: 16 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                    <h2 style={{ fontSize: 16, fontWeight: 600 }}>Günlük Satış</h2>
                    <span style={{ color: 'var(--text-secondary)', fontSize: 13 }}>
                      {report.dailyBreakdown.length} gün
                    </span>
                  </div>
                  <div style={{ overflowX: 'auto' }}>
                    <div style={{ display: 'flex', alignItems: 'flex-end', gap: 4, minWidth: Math.max(report.dailyBreakdown.length * 36, 300), height: 160, padding: '0 4px' }}>
                      {report.dailyBreakdown.map((day) => {
                        const pct = maxRev > 0 ? (day.revenue / maxRev) * 100 : 0;
                        return (
                          <div
                            key={day.date}
                            style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4 }}
                            title={`${fmtLongDate(day.date)}\n${formatCurrency(day.revenue)}\n${day.sessions} oturum`}
                          >
                            <div style={{
                              width: '100%',
                              height: `${Math.max(pct, day.revenue > 0 ? 4 : 0)}%`,
                              background: day.revenue > 0 ? 'var(--accent-primary)' : 'var(--bg-tertiary)',
                              borderRadius: '3px 3px 0 0',
                              transition: 'height 0.3s ease',
                              minHeight: day.revenue > 0 ? 4 : 2,
                              cursor: 'pointer',
                            }} />
                            <span style={{
                              fontSize: 9,
                              color: 'var(--text-muted)',
                              writingMode: report.dailyBreakdown.length > 14 ? 'vertical-rl' : 'horizontal-tb',
                              transform: report.dailyBreakdown.length > 14 ? 'rotate(180deg)' : 'none',
                              whiteSpace: 'nowrap',
                            }}>
                              {fmtDate(day.date)}
                            </span>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                </div>

                {/* Daily breakdown table */}
                <div className="card" style={{ marginBottom: 16 }}>
                  <h2 style={{ fontSize: 16, fontWeight: 600, marginBottom: 16 }}>Gün Gün Satış Detayı</h2>
                  <div style={{ overflowX: 'auto' }}>
                    <table className="data-table">
                      <thead>
                        <tr>
                          <th>TARİH</th>
                          <th>GELİR</th>
                          <th>OTURUM</th>
                          <th>SATIŞ BARINDA</th>
                        </tr>
                      </thead>
                      <tbody>
                        {report.dailyBreakdown.map((day) => (
                          <tr key={day.date} style={{ opacity: day.revenue === 0 ? 0.4 : 1 }}>
                            <td style={{ fontWeight: 500 }}>{fmtLongDate(day.date)}</td>
                            <td className="mono" style={{ fontWeight: 600, color: day.revenue > 0 ? 'var(--accent-success)' : 'var(--text-muted)' }}>
                              {day.revenue > 0 ? formatCurrency(day.revenue) : '—'}
                            </td>
                            <td>{day.sessions > 0 ? day.sessions : '—'}</td>
                            <td>
                              {day.revenue > 0 && maxRev > 0 && (
                                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                                  <div style={{ flex: 1, height: 6, background: 'var(--bg-tertiary)', borderRadius: 3, maxWidth: 120 }}>
                                    <div style={{ width: `${(day.revenue / maxRev) * 100}%`, height: '100%', background: 'var(--accent-primary)', borderRadius: 3 }} />
                                  </div>
                                  <span style={{ fontSize: 12, color: 'var(--text-secondary)' }}>
                                    {((day.revenue / report.totalRevenue) * 100).toFixed(1)}%
                                  </span>
                                </div>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                      <tfoot>
                        <tr style={{ fontWeight: 700, borderTop: '2px solid var(--border-color)' }}>
                          <td>TOPLAM</td>
                          <td className="mono" style={{ color: 'var(--accent-success)' }}>{formatCurrency(report.totalRevenue)}</td>
                          <td>{report.totalSessions}</td>
                          <td />
                        </tr>
                      </tfoot>
                    </table>
                  </div>
                </div>

                {/* Top items */}
                {report.topItems.length > 0 && (
                  <div className="card">
                    <h2 style={{ fontSize: 16, fontWeight: 600, marginBottom: 16 }}>En Çok Satan Ürünler</h2>
                    <table className="data-table">
                      <thead>
                        <tr><th>#</th><th>ÜRÜN</th><th>ADET</th><th>GELİR</th></tr>
                      </thead>
                      <tbody>
                        {report.topItems.map((item, i) => (
                          <tr key={item.itemName}>
                            <td><span className={`rank-badge${i < 3 ? ' top' : ''}`}>{i + 1}</span></td>
                            <td style={{ fontWeight: 500 }}>{item.itemName}</td>
                            <td>{item.quantity}</td>
                            <td className="mono" style={{ fontWeight: 600, color: 'var(--accent-success)' }}>
                              {formatCurrency(item.revenue)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </>
          )}

          {!report && !reportLoading && (
            <div className="card" style={{ textAlign: 'center', padding: '48px 32px', color: 'var(--text-secondary)' }}>
              <div style={{ fontSize: 40, marginBottom: 12 }}>📅</div>
              <p style={{ fontSize: 15, marginBottom: 8 }}>Tarih aralığı seçin ve "Raporla" tuşuna basın</p>
              <p style={{ fontSize: 13 }}>Gün gün satış, gelir ve ürün analizlerini görüntüleyin</p>
            </div>
          )}
        </>
      )}
    </div>
  );
}
