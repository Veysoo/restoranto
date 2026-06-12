import { useCallback, useEffect, useRef } from 'react';

/**
 * Polls `fn` every `intervalMs` ms.
 * - Pauses automatically when the browser tab is hidden.
 * - Calls immediately on mount.
 * - Safe: ignores in-flight responses if a new poll fires.
 */
export function usePolling(fn: () => Promise<void>, intervalMs: number, deps: unknown[] = []) {
  const running = useRef(false);

  const tick = useCallback(async () => {
    if (running.current || document.hidden) return;
    running.current = true;
    try { await fn(); } catch { /* caller handles errors */ } finally { running.current = false; }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  useEffect(() => {
    tick();
    const t = setInterval(tick, intervalMs);
    const onVisible = () => { if (!document.hidden) tick(); };
    document.addEventListener('visibilitychange', onVisible);
    return () => {
      clearInterval(t);
      document.removeEventListener('visibilitychange', onVisible);
    };
  }, [tick, intervalMs]);
}
