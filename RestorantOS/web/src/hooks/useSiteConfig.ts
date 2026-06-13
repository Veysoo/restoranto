import { useEffect, useState } from 'react';

export interface SiteConfig {
  siteUrl: string;
  siteHost: string;
}

function buildDefaults(): SiteConfig {
  const origin = window.location.origin;
  // If accessed from localhost, we'll try to get the LAN IP from the API
  const isLocal = origin.includes('localhost') || origin.includes('127.0.0.1');
  return {
    siteUrl: isLocal ? '' : origin,
    siteHost: isLocal ? '' : window.location.host,
  };
}

export function useSiteConfig() {
  const [config, setConfig] = useState<SiteConfig>(buildDefaults);

  useEffect(() => {
    // Always try to get the real LAN IP so QR works for mobile
    fetch('/api/health/network')
      .then((r) => (r.ok ? r.json() : null))
      .then((data) => {
        if (data?.url) {
          setConfig({ siteUrl: data.url, siteHost: data.ip + ':' + new URL(data.url).port });
        } else {
          // Fallback: use current window origin
          const origin = window.location.origin;
          setConfig({ siteUrl: origin, siteHost: window.location.host });
        }
      })
      .catch(() => {
        const origin = window.location.origin;
        setConfig({ siteUrl: origin, siteHost: window.location.host });
      });
  }, []);

  return config;
}
