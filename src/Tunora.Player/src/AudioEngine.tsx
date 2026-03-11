import { useEffect, useRef } from 'react';
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { kioskApi } from './kioskApi';
import { usePlayerStore } from './playerStore';

const HUB_URL = (import.meta.env.VITE_API_URL as string).replace('/api/v1', '') + '/hubs/playback';
const MAX_RETRIES = 5;

interface PlaybackCommand {
  type: 'Play' | 'Stop' | 'Next' | 'ChangeChannel';
  channelId?: number;
}

export default function AudioEngine() {
  const audioRef      = useRef<HTMLAudioElement>(null);
  const hubRef        = useRef<ReturnType<typeof HubConnectionBuilder.prototype.build> | null>(null);
  const connectedRef  = useRef(false);   // guard: hub is created exactly once
  const retryCountRef = useRef(0);
  const retryTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

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
      retryCountRef.current = 0;
      if (audioRef.current) {
        audioRef.current.src = track.audioUrl;
        audioRef.current.play().catch(console.error);
      }
      reportState('Playing', chId, track.trackId, track.title, track.artistName);
    } catch (err) {
      retryCountRef.current += 1;
      if (retryCountRef.current > MAX_RETRIES) {
        console.error('Max retries reached, stopping playback:', err);
        setError('Failed to load track after multiple attempts.');
        reportState('Stopped', chId, null, null, null);
        return;
      }
      const delay = Math.min(5000 * Math.pow(2, retryCountRef.current - 1), 60000);
      console.error(`Failed to fetch track (retry ${retryCountRef.current}/${MAX_RETRIES}) in ${delay}ms:`, err);
      setError(`Failed to load track — retrying in ${Math.round(delay / 1000)}s…`);
      retryTimerRef.current = setTimeout(() => playChannel(chId), delay);
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
          retryCountRef.current = 0;  // fresh play command resets backoff
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
    hub.onreconnecting(() => {
      usePlayerStore.getState().setHubStatus('reconnecting');
    });

    hub.onreconnected(() => {
      usePlayerStore.getState().setHubStatus('connected');
      const iId = usePlayerStore.getState().instanceId;
      if (iId) hub.invoke('JoinInstance', iId).catch(console.error);
    });

    hub.start()
      .then(() => {
        usePlayerStore.getState().setHubStatus('connected');
        hub.invoke('JoinInstance', instanceId);
      })
      .catch((err) => {
        connectedRef.current = false;   // allow retry on remount
        usePlayerStore.getState().setHubStatus('disconnected');
        usePlayerStore.getState().setError(`SignalR connection failed: ${err}`);
      });

    return () => {
      if (retryTimerRef.current !== null) {
        clearTimeout(retryTimerRef.current);
        retryTimerRef.current = null;
      }
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
