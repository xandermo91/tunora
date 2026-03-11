import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { instancesApi } from '../api/instances';
import { playbackApi } from '../api/playback';
import { useSignalRStore } from '../store/signalrStore';
import { useAuthStore } from '../store/authStore';
import ScheduleList from '../components/instances/ScheduleList';
import { parseApiError } from '../utils/errors';
import type { Instance } from '../types/instances';

const STATUS_COLOR: Record<string, string> = {
  Offline: 'text-sp-subtext',
  Online:  'text-blue-400',
  Playing: 'text-sp-green',
  Stopped: 'text-yellow-400',
};

export default function InstanceDetailPage() {
  const { id } = useParams<{ id: string }>();
  const instanceId = Number(id);
  const navigate = useNavigate();
  const token = useAuthStore(s => s.accessToken);

  const { connect, watchInstance, connection } = useSignalRStore();
  const [liveState, setLiveState] = useState<Partial<Instance>>({});

  const { data: instance, isLoading } = useQuery({
    queryKey: ['instances', instanceId],
    queryFn: () => instancesApi.get(instanceId).then(r => r.data),
    enabled: instanceId > 0,
  });

  // Merge live SignalR state over DB state
  const merged = instance ? { ...instance, ...liveState } : null;

  // Connect to SignalR and watch this instance
  useEffect(() => {
    if (!token) return;
    let mounted = true;
    connect(token).then(() => {
      if (mounted) watchInstance(instanceId);
    });
    return () => { mounted = false; };
  }, [token, instanceId, connect, watchInstance]);

  // Subscribe to state updates from the player
  useEffect(() => {
    if (!connection) return;
    const handler = (state: {
      instanceId: number; status: string; channelId: number | null;
      trackTitle: string | null; trackArtist: string | null;
    }) => {
      if (state.instanceId === instanceId) {
        setLiveState({
          status: state.status as Instance['status'],
          activeChannelId: state.channelId,
          currentTrackTitle: state.trackTitle,
          currentTrackArtist: state.trackArtist,
        });
      }
    };
    connection.on('ReceiveState', handler);
    return () => connection.off('ReceiveState', handler);
  }, [connection, instanceId]);

  const [selectedChannelId, setSelectedChannelId] = useState<number | null>(null);
  const [playbackError, setPlaybackError] = useState<string | null>(null);

  // Default selected channel to the instance's active channel, then first assigned
  useEffect(() => {
    if (selectedChannelId) return;
    if (merged?.activeChannelId) setSelectedChannelId(merged.activeChannelId);
    else if (merged?.channels?.[0]) setSelectedChannelId(merged.channels[0].channelId);
  }, [merged?.activeChannelId, merged?.channels, selectedChannelId]);

  const handlePlaybackError = (err: unknown) => setPlaybackError(parseApiError(err, 'Playback command failed.'));
  const clearPlaybackError = () => setPlaybackError(null);

  const playMutation   = useMutation({ mutationFn: () => playbackApi.play(instanceId, selectedChannelId!), onMutate: clearPlaybackError, onError: handlePlaybackError });
  const stopMutation   = useMutation({ mutationFn: () => playbackApi.stop(instanceId), onMutate: clearPlaybackError, onError: handlePlaybackError });
  const nextMutation   = useMutation({ mutationFn: () => playbackApi.next(instanceId), onMutate: clearPlaybackError, onError: handlePlaybackError });
  const changeMutation = useMutation({
    mutationFn: (channelId: number) => playbackApi.changeChannel(instanceId, channelId),
    onSuccess: (_, channelId) => setSelectedChannelId(channelId),
    onMutate: clearPlaybackError,
    onError: handlePlaybackError,
  });

  const isPlaying = merged?.status === 'Playing';

  if (isLoading) return <div className="p-8 text-sp-subtext text-center py-16">Loading…</div>;
  if (!merged)   return <div className="p-8 text-sp-subtext text-center py-16">Instance not found.</div>;

  return (
    <div className="p-8 max-w-3xl">

      {/* Breadcrumb */}
      <button onClick={() => navigate('/instances')} className="text-sp-subtext text-sm hover:text-sp-white mb-6 flex items-center gap-1.5">
        ← Locations
      </button>

      {/* Header */}
      <div className="mb-8">
        <h1 className="text-sp-white text-2xl font-bold">{merged.name}</h1>
        <p className="text-sp-subtext text-sm mt-0.5">{merged.location}</p>
        <div className="flex items-center gap-2 mt-2">
          <span className={`text-sm font-medium ${STATUS_COLOR[merged.status] ?? STATUS_COLOR.Offline}`}>
            {merged.status}
          </span>
          {merged.lastSeenAt && (
            <span className="text-sp-subtext text-xs">
              · Last seen {new Date(merged.lastSeenAt).toLocaleTimeString()}
            </span>
          )}
        </div>
      </div>

      {/* Now Playing */}
      {isPlaying && (
        <div className="bg-sp-gray rounded-xl px-6 py-5 mb-6 flex items-center gap-4">
          <div className="w-10 h-10 rounded-full bg-sp-green/20 flex items-center justify-center text-sp-green text-lg">
            ♪
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sp-white font-medium text-sm truncate">
              {merged.currentTrackTitle ?? 'Loading track…'}
            </p>
            <p className="text-sp-subtext text-xs truncate">
              {merged.currentTrackArtist ?? ''}
            </p>
          </div>
        </div>
      )}

      {/* Playback Controls */}
      <div className="bg-sp-gray rounded-xl px-6 py-5 mb-6">
        <h2 className="text-sp-white font-semibold text-sm mb-4">Playback Controls</h2>

        {merged.channels.length === 0 ? (
          <p className="text-sp-subtext text-sm">No channels assigned. Go to Locations to assign channels first.</p>
        ) : (
          <>
            {/* Channel selector */}
            <div className="flex flex-wrap gap-2 mb-5">
              {merged.channels.map(ch => (
                <button
                  key={ch.channelId}
                  onClick={() => {
                    setSelectedChannelId(ch.channelId);
                    if (isPlaying) changeMutation.mutate(ch.channelId);
                  }}
                  className={`flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-medium transition-colors border ${
                    selectedChannelId === ch.channelId
                      ? 'border-sp-green/60 bg-sp-green/15 text-sp-white'
                      : 'border-sp-lightgray/40 bg-sp-darkgray text-sp-subtext hover:text-sp-white'
                  }`}
                >
                  <span className="w-2 h-2 rounded-full" style={{ backgroundColor: ch.accentColor }} />
                  {ch.name}
                </button>
              ))}
            </div>

            {/* Playback error */}
            {playbackError && (
              <p className="text-red-400 text-xs mb-3">{playbackError}</p>
            )}

            {/* Play / Stop / Next */}
            <div className="flex items-center gap-3">
              {isPlaying ? (
                <button
                  onClick={() => stopMutation.mutate()}
                  disabled={stopMutation.isPending}
                  className="bg-red-500/20 hover:bg-red-500/30 text-red-400 font-bold px-6 py-2.5 rounded-full text-sm transition-colors disabled:opacity-60"
                >
                  ■ Stop
                </button>
              ) : (
                <button
                  onClick={() => playMutation.mutate()}
                  disabled={!selectedChannelId || playMutation.isPending}
                  className="bg-sp-green hover:bg-sp-green-hover text-sp-black font-bold px-6 py-2.5 rounded-full text-sm transition-colors disabled:opacity-60"
                >
                  ▶ Play
                </button>
              )}

              {isPlaying && (
                <button
                  onClick={() => nextMutation.mutate()}
                  disabled={nextMutation.isPending}
                  className="bg-sp-lightgray hover:bg-sp-lightgray/80 text-sp-white font-bold px-5 py-2.5 rounded-full text-sm transition-colors disabled:opacity-40"
                >
                  ⏭ Next
                </button>
              )}
            </div>
          </>
        )}
      </div>

      {/* Connection Key */}
      <div className="bg-sp-gray rounded-xl px-6 py-5 mb-6">
        <h2 className="text-sp-white font-semibold text-sm mb-1">Player Setup</h2>
        <p className="text-sp-subtext text-xs mb-3">
          Open the player URL in a browser (kiosk mode), then enter the Connection Key to pair it with this location.
          Player URL: <code className="text-sp-subtext font-mono">{import.meta.env.VITE_PLAYER_URL ?? 'http://localhost:5175'}/?key=&lt;ConnectionKey&gt;</code>
        </p>
        <RevealConnectionKey instanceId={instanceId} />
      </div>

      {/* Schedules */}
      <ScheduleList instanceId={instanceId} />

    </div>
  );
}

function RevealConnectionKey({ instanceId }: { instanceId: number }) {
  const [key, setKey] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const reveal = async () => {
    setLoading(true);
    try {
      const r = await instancesApi.getConnectionKey(instanceId);
      setKey(r.data.connectionKey);
    } finally {
      setLoading(false);
    }
  };

  if (key) {
    return (
      <div className="flex gap-2">
        <code className="flex-1 bg-sp-darkgray text-sp-subtext text-xs px-3 py-2 rounded font-mono break-all">
          {key}
        </code>
        <button
          onClick={() => navigator.clipboard.writeText(key)}
          className="text-sp-subtext hover:text-sp-white text-xs px-3 py-2 bg-sp-lightgray rounded transition-colors whitespace-nowrap"
        >
          Copy
        </button>
      </div>
    );
  }

  return (
    <button
      onClick={reveal}
      disabled={loading}
      className="text-sp-subtext hover:text-sp-white text-xs border border-sp-lightgray/40 px-4 py-2 rounded transition-colors disabled:opacity-60"
    >
      {loading ? 'Loading…' : 'Reveal Connection Key'}
    </button>
  );
}
