import { useEffect, useState } from 'react';
import {
  isMobileDevice,
  isPrivateLanHost,
  probeHealth,
  redirectToServer,
  saveServerUrl,
  scanForServer,
} from '../lib/mobilDiscovery';

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

    const host = window.location.hostname;

    // Telefon zaten LAN IP ile sunucuya bağlandıysa direkt göster
    if (isPrivateLanHost(host) || host === 'localhost' || host === '127.0.0.1') {
      setPhase('ready');
      return;
    }

    // Farklı bir adresten açılmışsa health check yap
    let cancelled = false;
    const tryConnect = async () => {
      for (let attempt = 0; attempt < 3; attempt++) {
        if (cancelled) return;
        const ok = await probeHealth(window.location.origin, 2500);
        if (ok) {
          if (!cancelled) setPhase('ready');
          return;
        }
      }
      if (!cancelled) runScan();
    };

    tryConnect();
    return () => { cancelled = true; };
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
