import { useEffect, useState } from 'react';

export interface SiteConfig {
  siteUrl: string;
  siteHost: string;
}

function buildFromOrigin(): SiteConfig {
  const origin = window.location.origin;
  return {
    siteUrl: origin,
    siteHost: window.location.host,
  };
}

export function useSiteConfig() {
  const [config, setConfig] = useState<SiteConfig>(buildFromOrigin);

  useEffect(() => {
    const ctrl = new AbortController();

    fetch('/api/health/network', { signal: ctrl.signal })
      .then((r) => (r.ok ? r.json() : null))
      .then((data) => {
        if (data?.url && data?.ip) {
          try {
            const port = new URL(data.url).port || '8080';
            setConfig({ siteUrl: data.url, siteHost: `${data.ip}:${port}` });
          } catch {
            setConfig(buildFromOrigin());
          }
        }
      })
      .catch(() => {
        // API'den yanıt gelmezse mevcut origin'i kullan
      });

    return () => ctrl.abort();
  }, []);

  return config;
}
