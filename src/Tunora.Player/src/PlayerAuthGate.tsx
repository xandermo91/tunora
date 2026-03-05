import { useState, useEffect } from 'react';
import { kioskApi } from './kioskApi';
import { usePlayerStore } from './playerStore';

export default function PlayerAuthGate({ children }: { children: React.ReactNode }) {
  const { token, setAuth } = usePlayerStore();
  const [loading, setLoading] = useState(false);
  const [authError, setAuthError] = useState('');

  // Auto-authenticate if ConnectionKey is in the URL query string
  useEffect(() => {
    const key = new URLSearchParams(window.location.search).get('key');
    if (key && !token) authenticate(key);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const [manualKey, setManualKey] = useState('');

  const authenticate = async (key: string) => {
    setLoading(true);
    setAuthError('');
    try {
      const result = await kioskApi.authenticate(key);
      setAuth(result.accessToken, result.instanceId, result.instanceName);
    } catch {
      setAuthError('Invalid connection key — please check and try again.');
      setLoading(false);
    }
  };

  if (token) return <>{children}</>;

  return (
    <div style={{
      minHeight: '100vh',
      background: '#121212',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontFamily: 'system-ui, sans-serif',
      color: '#fff',
    }}>
      <div style={{ textAlign: 'center', padding: '2rem', maxWidth: '380px', width: '100%' }}>
        <div style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '0.5rem' }}>Tunora Player</div>
        <div style={{ color: '#b3b3b3', fontSize: '0.875rem', marginBottom: authError ? '1rem' : '2rem' }}>
          {loading ? 'Authenticating…' : 'Enter your Connection Key to pair this player.'}
        </div>
        {authError && (
          <div style={{ color: '#ff6b6b', fontSize: '0.8rem', marginBottom: '1rem', padding: '0.5rem', background: '#2a1515', borderRadius: '6px' }}>
            {authError}
          </div>
        )}

        {!loading && (
          <form onSubmit={(e) => { e.preventDefault(); authenticate(manualKey.trim()); }}>
            <input
              type="text"
              value={manualKey}
              onChange={e => setManualKey(e.target.value)}
              placeholder="Paste connection key…"
              style={{
                width: '100%',
                padding: '0.75rem 1rem',
                borderRadius: '6px',
                border: '1px solid #333',
                background: '#282828',
                color: '#fff',
                fontSize: '0.875rem',
                marginBottom: '1rem',
                boxSizing: 'border-box',
                fontFamily: 'monospace',
              }}
            />
            <button
              type="submit"
              disabled={!manualKey.trim()}
              style={{
                width: '100%',
                padding: '0.75rem',
                borderRadius: '24px',
                border: 'none',
                background: '#1DB954',
                color: '#000',
                fontWeight: 700,
                fontSize: '0.875rem',
                cursor: manualKey.trim() ? 'pointer' : 'not-allowed',
                opacity: manualKey.trim() ? 1 : 0.5,
              }}
            >
              Connect
            </button>
          </form>
        )}
      </div>
    </div>
  );
}
