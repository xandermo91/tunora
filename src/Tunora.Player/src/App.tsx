import PlayerAuthGate from './PlayerAuthGate';
import AudioEngine from './AudioEngine';
import NowPlayingScreen from './NowPlayingScreen';
import { usePlayerStore } from './playerStore';

export default function App() {
  const error = usePlayerStore(s => s.error);

  return (
    <PlayerAuthGate>
      <AudioEngine />
      {error ? (
        <div style={{
          minHeight: '100vh', background: '#121212', display: 'flex',
          alignItems: 'center', justifyContent: 'center',
          fontFamily: 'system-ui, sans-serif', color: '#ff4444',
          textAlign: 'center', padding: '2rem',
        }}>
          <div>
            <div style={{ fontSize: '2rem', marginBottom: '1rem' }}>⚠</div>
            <div>{error}</div>
          </div>
        </div>
      ) : (
        <NowPlayingScreen />
      )}
    </PlayerAuthGate>
  );
}
