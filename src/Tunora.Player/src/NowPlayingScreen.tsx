import { usePlayerStore } from './playerStore';
import { resumeAudio } from './AudioEngine';

export default function NowPlayingScreen() {
  const { instanceName, status, hubStatus, currentTrack, channelId, autoplayBlocked } = usePlayerStore();

  return (
    <div style={{
      position: 'relative',
      minHeight: '100vh',
      background: '#121212',
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      fontFamily: 'system-ui, sans-serif',
      color: '#fff',
      padding: '2rem',
      textAlign: 'center',
    }}>
      {/* Autoplay blocked — full-screen tap-to-play overlay */}
      {autoplayBlocked && (
        <div
          onClick={() => resumeAudio?.()}
          style={{
            position: 'absolute',
            inset: 0,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            background: 'rgba(0,0,0,0.75)',
            cursor: 'pointer',
            zIndex: 10,
          }}
        >
          <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>▶</div>
          <div style={{ fontSize: '1rem', fontWeight: 600, letterSpacing: '0.05em' }}>Tap to start audio</div>
          <div style={{ fontSize: '0.75rem', color: '#b3b3b3', marginTop: '0.5rem' }}>Browser requires a click to enable sound</div>
        </div>
      )}

      {/* Reconnecting indicator */}
      {hubStatus === 'reconnecting' && (
        <div style={{
          position: 'absolute',
          top: '1.5rem',
          right: '1.5rem',
          background: '#b45309',
          color: '#fef3c7',
          fontSize: '0.7rem',
          fontWeight: 600,
          padding: '0.25rem 0.75rem',
          borderRadius: '9999px',
          letterSpacing: '0.05em',
        }}>
          Reconnecting…
        </div>
      )}

      {/* Logo area */}
      <div style={{ marginBottom: '3rem', opacity: 0.4 }}>
        <div style={{ fontSize: '1rem', letterSpacing: '0.3em', textTransform: 'uppercase' }}>
          Tunora
        </div>
        {instanceName && (
          <div style={{ fontSize: '0.75rem', color: '#b3b3b3', marginTop: '0.25rem' }}>
            {instanceName}
          </div>
        )}
      </div>

      {/* Album art placeholder */}
      <div style={{
        width: '220px',
        height: '220px',
        borderRadius: '8px',
        background: status === 'playing' ? '#282828' : '#1a1a1a',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        marginBottom: '2rem',
        overflow: 'hidden',
        boxShadow: status === 'playing' ? '0 8px 40px rgba(29,185,84,0.15)' : 'none',
        transition: 'all 0.4s ease',
      }}>
        {currentTrack?.albumImageUrl ? (
          <img
            src={currentTrack.albumImageUrl}
            alt="Album art"
            style={{ width: '100%', height: '100%', objectFit: 'cover' }}
          />
        ) : (
          <span style={{ fontSize: '4rem', opacity: 0.3 }}>♪</span>
        )}
      </div>

      {/* Track info */}
      {status === 'playing' && currentTrack ? (
        <>
          <div style={{ fontSize: '1.25rem', fontWeight: 700, marginBottom: '0.4rem', maxWidth: '400px' }}>
            {currentTrack.title}
          </div>
          <div style={{ fontSize: '0.875rem', color: '#b3b3b3' }}>
            {currentTrack.artistName}
          </div>
          <div style={{ marginTop: '1.5rem', color: '#1DB954', fontSize: '0.75rem', letterSpacing: '0.1em' }}>
            ● PLAYING
          </div>
        </>
      ) : status === 'stopped' ? (
        <div style={{ color: '#b3b3b3', fontSize: '0.875rem' }}>Paused</div>
      ) : (
        <div style={{ color: '#b3b3b3', fontSize: '0.875rem' }}>
          {status === 'waiting' ? 'Waiting for play command…' : 'Connecting…'}
          {channelId && status === 'waiting' && (
            <div style={{ fontSize: '0.75rem', marginTop: '0.5rem', opacity: 0.6 }}>
              Channel ready · awaiting dashboard control
            </div>
          )}
        </div>
      )}
    </div>
  );
}
