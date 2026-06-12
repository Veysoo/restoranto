import { FormEvent, useState } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api';
import { useSiteConfig } from '../hooks/useSiteConfig';

export default function LoginPage() {
  const { siteUrl, siteHost } = useSiteConfig();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      await api.login(username, password);
      navigate('/floor');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Kullanıcı adı veya şifre hatalı.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-wrap">
        {/* Sol taraf: giriş formu */}
        <div className="card login-card">
          <div className="login-logo">
            <div className="login-logo-icon">R</div>
            <div>
              <div className="login-logo-text">RestaurantOS</div>
              <div className="login-logo-sub">Restoran Yönetim Sistemi</div>
            </div>
          </div>

          <h2>Hoş Geldiniz</h2>
          <p className="login-desc">Hesabınızla giriş yapın</p>

          {error && <div className="error-msg">{error}</div>}

          <form onSubmit={submit}>
            <div className="form-group">
              <label>Kullanıcı Adı</label>
              <input
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                autoComplete="username"
                placeholder="kullanıcı adınız"
                required
              />
            </div>
            <div className="form-group">
              <label>Şifre</label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="current-password"
                placeholder="••••••••"
                required
              />
            </div>
            <button className="btn-primary" type="submit" disabled={loading} style={{ width: '100%', marginTop: 8, justifyContent: 'center' }}>
              {loading ? 'Giriş yapılıyor…' : 'Giriş Yap →'}
            </button>
          </form>
        </div>

        {/* Sağ taraf: QR + özellikler */}
        <div className="login-hero">
          {siteUrl && (
            <div className="qr-panel">
              <div className="qr-panel-title">📱 Mobil Erişim</div>
              <div className="qr-panel-url">{siteHost}</div>
              <div className="qr-wrap">
                <QRCodeSVG
                  value={`${siteUrl.replace(/\/$/, '')}/mobil.html`}
                  size={150}
                  bgColor="#ffffff"
                  fgColor="#0f4c81"
                  level="M"
                />
              </div>
              <p className="qr-hint">
                Telefonla QR kodu tarayın — sunucu otomatik bulunur.<br />
                Sonra <strong>Ana ekrana ekle</strong> ile kısayol oluşturun.
              </p>
            </div>
          )}

          <div className="login-features">
            <div className="login-feature-item">
              <span className="login-feature-icon">🏠</span>
              <span>Canlı kat planı ile masa durumları anlık güncellenir</span>
            </div>
            <div className="login-feature-item">
              <span className="login-feature-icon">🍽️</span>
              <span>Sipariş ekle, ödeme al, mutfak kanban ile takip et</span>
            </div>
            <div className="login-feature-item">
              <span className="login-feature-icon">📊</span>
              <span>Günlük gelir ve satış analizlerini görüntüle</span>
            </div>
            <div className="login-feature-item">
              <span className="login-feature-icon">🔄</span>
              <span>Ağdaki tüm cihazlar eş zamanlı çalışır</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
