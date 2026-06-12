/** Mobil cihazlarda ağdaki RestaurantOS sunucusunu otomatik bulur */

export const SERVER_STORAGE_KEY = 'restaurantos_server_url';

export function isMobileDevice(): boolean {
  if (typeof navigator === 'undefined') return false;
  return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini|Mobile/i.test(navigator.userAgent)
    || (navigator.maxTouchPoints > 1 && window.innerWidth < 1024);
}

export function isPrivateLanHost(host: string): boolean {
  return /^(192\.168\.|10\.|172\.(1[6-9]|2[0-9]|3[0-1])\.)/.test(host);
}

export function getSavedServerUrl(): string | null {
  try {
    return localStorage.getItem(SERVER_STORAGE_KEY);
  } catch {
    return null;
  }
}

export function saveServerUrl(url: string) {
  localStorage.setItem(SERVER_STORAGE_KEY, url.replace(/\/$/, ''));
}

export async function probeHealth(base: string, timeoutMs = 1200): Promise<boolean> {
  const ctrl = new AbortController();
  const t = setTimeout(() => ctrl.abort(), timeoutMs);
  try {
    const res = await fetch(`${base.replace(/\/$/, '')}/api/health`, { signal: ctrl.signal });
    if (!res.ok) return false;
    const data = await res.json();
    return data?.status === 'ok';
  } catch {
    return false;
  } finally {
    clearTimeout(t);
  }
}

function getLocalIpViaWebRtc(): Promise<string | null> {
  return new Promise((resolve) => {
    const RTCPeer = window.RTCPeerConnection
      || (window as unknown as { webkitRTCPeerConnection: typeof RTCPeerConnection }).webkitRTCPeerConnection;
    if (!RTCPeer) {
      resolve(null);
      return;
    }
    const pc = new RTCPeer({ iceServers: [] });
    pc.createDataChannel('');
    pc.createOffer().then((o) => pc.setLocalDescription(o)).catch(() => resolve(null));
    pc.onicecandidate = (e) => {
      if (!e.candidate?.candidate) return;
      const m = /([0-9]{1,3}(?:\.[0-9]{1,3}){3})/.exec(e.candidate.candidate);
      if (m && !m[1].startsWith('127.') && !m[1].startsWith('169.254.')) {
        pc.close();
        resolve(m[1]);
      }
    };
    setTimeout(() => { pc.close(); resolve(null); }, 2500);
  });
}

function subnetFromIp(ip: string) {
  return ip.replace(/\.\d+$/, '');
}

const COMMON_SUBNETS = ['192.168.1', '192.168.0', '192.168.43', '192.168.50', '10.0.0', '172.20.10'];

export async function scanForServer(
  onProgress?: (pct: number) => void,
  subnetHint?: string,
): Promise<string | null> {
  const origin = window.location.origin;
  const host = window.location.hostname;

  if (isPrivateLanHost(host)) {
    if (await probeHealth(origin)) return origin.replace(/\/$/, '');
  }

  const saved = getSavedServerUrl();
  if (saved && saved !== origin && await probeHealth(saved)) return saved.replace(/\/$/, '');

  const subnets = new Set<string>();
  if (subnetHint?.trim()) subnets.add(subnetHint.trim());
  const localIp = await getLocalIpViaWebRtc();
  if (localIp) subnets.add(subnetFromIp(localIp));
  COMMON_SUBNETS.forEach((s) => subnets.add(s));

  const ports = [8080, 80];
  const list = [...subnets];
  let step = 0;
  const totalSteps = list.length * 254;

  for (const subnet of list) {
    for (let batch = 0; batch < 254; batch += 20) {
      const hosts = Array.from({ length: Math.min(20, 254 - batch) }, (_, i) => batch + i + 1);
      const results = await Promise.all(
        hosts.flatMap((h) =>
          ports.map(async (port) => {
            const base = `http://${subnet}.${h}:${port}`;
            return (await probeHealth(base, 800)) ? base : null;
          }),
        ),
      );
      step += hosts.length;
      onProgress?.(Math.min(99, Math.round((step / totalSteps) * 100)));
      const hit = results.find(Boolean);
      if (hit) {
        onProgress?.(100);
        return hit;
      }
    }
  }
  onProgress?.(100);
  return null;
}

export function redirectToServer(serverUrl: string, path = '/login') {
  const base = serverUrl.replace(/\/$/, '');
  const target = `${base}${path.startsWith('/') ? path : `/${path}`}`;
  if (window.location.href.startsWith(base)) return;
  window.location.replace(target);
}

/** Mobilde kayıtlı sunucuya yönlendir veya tarama gerekip gerekmediğini döndür */
export async function ensureMobileServer(): Promise<'ok' | 'scan' | 'redirecting'> {
  if (!isMobileDevice()) return 'ok';

  const origin = window.location.origin;
  if (await probeHealth(origin)) return 'ok';

  const saved = getSavedServerUrl();
  if (saved && saved !== origin && await probeHealth(saved)) {
    redirectToServer(saved);
    return 'redirecting';
  }

  return 'scan';
}
