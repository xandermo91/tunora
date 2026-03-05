import { useEffect, useRef } from 'react';
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { kioskApi } from './kioskApi';
import { usePlayerStore } from './playerStore';

const HUB_URL = (import.meta.env.VITE_API_URL as string).replace('/api/v1', '') + '/hubs/playback';

interface PlaybackCommand {
  type: 'Play' | 'Stop' | 'Next' | 'ChangeChannel';
  channelId?: number;
}

export default function AudioEngine() {
  const audioRef     = useRef<HTMLAudioElement>(null);
  const hubRef       = useRef<ReturnType<typeof HubConnectionBuilder.prototype.build> | null>(null);
  const connectedRef = useRef(false);   // guard: hub is created exactly once

  // ── Always read the latest store values from inside handlers ────────────────
  const { token, instanceId } = usePlayerStore();

  // ── Report state back to dashboard ─────────────────────────────────────────
  const reportState = (status: string, chId: number | null, trackId: string | null, title: string | null, artist: string | null) => {
    const hub = hubRef.current;
    const iId = usePlayerStore.getState().instanceId;
    if (hub?.state !== HubConnectionState.Connected || !iId) return;
    const albumImageUrl = usePlayerStore.getState().currentTrack?.albumImageUrl ?? null;
    hub.invoke('ReportState', { instanceId: iId, status, channelId: chId, trackId, trackTitle: title, trackArtist: artist, albumImageUrl })
       .catch(console.error);
  };

  // ── Fetch and play a track ──────────────────────────────────────────────────
  const playChannel = async (chId: number) => {
    const { token: tok, setStatus, setCurrentTrack, setError } = usePlayerStore.getState();
    if (!tok) return;
    setStatus('playing');
    try {
      const track = await kioskApi.nextTrack(chId, tok);
      const current = { trackId: track.trackId, title: track.title, artistName: track.artistName, audioUrl: track.audioUrl, albumImageUrl: track.albumImageUrl };
      setCurrentTrack(current);
      if (audioRef.current) {
        audioRef.current.src = track.audioUrl;
        audioRef.current.play().catch(console.error);
      }
      reportState('Playing', chId, track.trackId, track.title, track.artistName);
    } catch (err) {
      console.error('Failed to fetch track:', err);
      setError('Failed to load track — retrying in 5s…');
      setTimeout(() => playChannel(chId), 5000);
    }
  };

  // ── SignalR — created once when token + instanceId are ready ────────────────
  useEffect(() => {
    if (!token || !instanceId || connectedRef.current) return;
    connectedRef.current = true;

    const hub = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => usePlayerStore.getState().token! })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    hubRef.current = hub;

    hub.on('ReceiveCommand', (cmd: PlaybackCommand) => {
      const state = usePlayerStore.getState();
      const chId  = cmd.channelId ?? state.channelId;

      switch (cmd.type) {
        case 'Play':
          if (cmd.channelId) state.setChannelId(cmd.channelId);
          playChannel(cmd.channelId ?? chId!);
          break;
        case 'Stop':
          audioRef.current?.pause();
          if (audioRef.current) audioRef.current.src = '';  // release media resource
          state.setStatus('stopped');
          state.setCurrentTrack(null);
          reportState('Stopped', chId ?? null, null, null, null);
          break;
        case 'Next':
          if (chId) playChannel(chId);
          break;
        case 'ChangeChannel':
          if (cmd.channelId) {
            state.setChannelId(cmd.channelId);
            playChannel(cmd.channelId);
          }
          break;
      }
    });

    // Re-join instance group after automatic reconnect
    hub.onreconnected(() => {
      const iId = usePlayerStore.getState().instanceId;
      if (iId) hub.invoke('JoinInstance', iId).catch(console.error);
    });

    hub.start()
      .then(() => hub.invoke('JoinInstance', instanceId))
      .catch((err) => {
        connectedRef.current = false;   // allow retry on remount
        usePlayerStore.getState().setError(`SignalR connection failed: ${err}`);
      });

    return () => {
      hub.stop();
      hubRef.current = null;
      connectedRef.current = false;
    };
  }, [token, instanceId]); // eslint-disable-line react-hooks/exhaustive-deps

  return (
    // eslint-disable-next-line jsx-a11y/media-has-caption
    <audio
      ref={audioRef}
      onEnded={() => {
        const { channelId: chId } = usePlayerStore.getState();
        if (chId) playChannel(chId);
      }}
      style={{ display: 'none' }}
      crossOrigin="anonymous"
    />
  );
}
