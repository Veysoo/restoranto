import { useEffect, useState } from 'react';
import {
  isMobileDevice,
  probeHealth,
  redirectToServer,
  saveServerUrl,
  scanForServer,
} from '../lib/mobilDiscovery';

/** Mobilde uygulama açılınca sunucu yoksa otomatik tarama ekranı */
export default function MobileAutoConnect({ children }: { children: React.ReactNode }) {
  const [phase, setPhase] = useState<'checking' | 'scanning' | 'ready' | 'error'>('checking');
  const [progress, setProgress] = useState(0);
  const [foundUrl, setFoundUrl] = useState('');
  const [subnet, setSubnet] = useState('');
  const [error, setError] = useState('');

  const runScan = async (hint?: string) => {
    setPhase('scanning');
    setError('');
    setProgress(0);
    const url = await scanForServer(setProgress, hint);
    if (url) {
      saveServerUrl(url);
      setFoundUrl(url);
      setPhase('ready');
      setTimeout(() => redirectToServer(url, '/login'), 600);
    } else {
      setPhase('error');
      setError('Ağda RestaurantOS bulunamadı. Sunucu PC açık ve aynı WiFi\'da olmalı.');
    }
  };

  useEffect(() => {
    if (!isMobileDevice()) {
      setPhase('ready');
      return;
    }
    probeHealth(window.location.origin).then((ok) => {
      if (ok) {
        setPhase('ready');
        return;
      }
      runScan();
    });
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  if (phase === 'ready') return <>{children}</>;

  return (
    <div className="mobile-connect-screen">
      <div className="mobile-connect-card">
        <div className="mobile-connect-logo">R</div>
        <h1>RestaurantOS</h1>
        <p className="mobile-connect-sub">Mobil bağlantı kuruluyor…</p>

        {phase === 'checking' || phase === 'scanning' ? (
          <>
            <div className="mobile-connect-spinner" />
            <p className="mobile-connect-status">
              {phase === 'checking' ? 'Kontrol ediliyor…' : 'Ağdaki sunucu aranıyor…'}
            </p>
            <div className="mobile-connect-bar">
              <div className="mobile-connect-bar-fill" style={{ width: `${progress}%` }} />
            </div>
            <span className="mobile-connect-pct">%{progress}</span>
          </>
        ) : null}

        {phase === 'error' ? (
          <>
            <div className="error-msg">{error}</div>
            <div className="form-group">
              <label>Ağ öneki (opsiyonel)</label>
              <input
                placeholder="192.168.1"
                value={subnet}
                onChange={(e) => setSubnet(e.target.value)}
              />
            </div>
            <button type="button" className="btn-primary mobile-connect-btn" onClick={() => runScan(subnet)}>
              Tekrar Ara
            </button>
          </>
        ) : null}

        {foundUrl && phase !== 'error' ? (
          <p className="mobile-connect-found">✓ Bulundu: {foundUrl}</p>
        ) : null}
      </div>
    </div>
  );
}
