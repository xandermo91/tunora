import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { instancesApi } from '../../api/instances';
import { channelsApi } from '../../api/channels';
import type { Instance, InstanceCreated } from '../../types/instances';

interface Props {
  instance: Instance | null;
  onClose: () => void;
}

export default function InstanceModal({ instance, onClose }: Props) {
  const qc = useQueryClient();
  const isEdit = instance !== null;

  const [name, setName] = useState(instance?.name ?? '');
  const [location, setLocation] = useState(instance?.location ?? '');
  const [error, setError] = useState('');
  const [channelError, setChannelError] = useState('');
  const [saved, setSaved] = useState<Instance | null>(instance);
  const [connectionKey, setConnectionKey] = useState<string | null>(null);
  const [step, setStep] = useState<'info' | 'channels'>(isEdit ? 'channels' : 'info');

  const { data: channels = [] } = useQuery({
    queryKey: ['channels'],
    queryFn: () => channelsApi.list().then(r => r.data),
  });

  const assignedIds = new Set(saved?.channels.map(c => c.channelId) ?? []);

  const saveMutation = useMutation({
    mutationFn: (): Promise<Instance | InstanceCreated> =>
      isEdit
        ? instancesApi.update(instance!.id, { name, location }).then(r => r.data)
        : instancesApi.create({ name, location }).then(r => r.data),
    onSuccess: (data) => {
      if (!isEdit && 'connectionKey' in data) setConnectionKey(data.connectionKey);
      // Strip connectionKey — Instance type doesn't carry it after the first response
      const { connectionKey: _ck, ...rest } = data as typeof data & { connectionKey?: string };
      void _ck;
      setSaved(rest as Instance);
      qc.invalidateQueries({ queryKey: ['instances'] });
      setStep('channels');
    },
    onError: (err: unknown) => {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      setError(msg ?? 'Something went wrong.');
    },
  });

  const toggleChannel = async (channelId: number) => {
    if (!saved) return;
    setChannelError('');
    try {
      if (assignedIds.has(channelId)) {
        await instancesApi.removeChannel(saved.id, channelId);
      } else {
        await instancesApi.assignChannel(saved.id, channelId);
      }
      const updated = await instancesApi.get(saved.id).then(r => r.data);
      setSaved(updated);
      qc.invalidateQueries({ queryKey: ['instances'] });
    } catch {
      setChannelError('Failed to update channel. Please try again.');
    }
  };

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
      <div className="bg-sp-gray rounded-xl w-full max-w-md shadow-2xl">

        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-sp-lightgray/30">
          <h2 className="text-sp-white font-bold text-lg">
            {isEdit ? 'Edit Location' : 'Add Location'}
          </h2>
          <button onClick={onClose} className="text-sp-subtext hover:text-sp-white text-2xl leading-none">×</button>
        </div>

        <div className="px-6 py-5 space-y-4">

          {/* Step 1 — Basic info */}
          {step === 'info' && (
            <>
              <div>
                <label className="block text-sp-subtext text-sm mb-1.5">Location name</label>
                <input
                  type="text"
                  value={name}
                  onChange={e => setName(e.target.value)}
                  maxLength={200}
                  placeholder="Main Store, Branch 3…"
                  className="w-full bg-sp-lightgray text-sp-white rounded px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-sp-green placeholder-sp-subtext"
                />
              </div>
              <div>
                <label className="block text-sp-subtext text-sm mb-1.5">Physical address</label>
                <input
                  type="text"
                  value={location}
                  onChange={e => setLocation(e.target.value)}
                  maxLength={500}
                  placeholder="123 Main St, Springfield"
                  className="w-full bg-sp-lightgray text-sp-white rounded px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-sp-green placeholder-sp-subtext"
                />
              </div>
              {error && <p className="text-red-400 text-sm">{error}</p>}
            </>
          )}

          {/* Step 2 — Channels */}
          {step === 'channels' && (
            <>
              {/* Summary card */}
              <div className="bg-sp-darkgray rounded-lg px-4 py-3 flex items-center justify-between">
                <div>
                  <p className="text-sp-white text-sm font-medium">{saved?.name}</p>
                  <p className="text-sp-subtext text-xs">{saved?.location}</p>
                </div>
                {!isEdit && (
                  <button onClick={() => setStep('info')} className="text-sp-subtext text-xs hover:text-sp-white">
                    Edit
                  </button>
                )}
              </div>

              {/* Connection key — shown only right after creation */}
              {connectionKey && (
                <div>
                  <label className="block text-sp-subtext text-sm mb-1.5">Connection Key</label>
                  <div className="flex gap-2">
                    <code className="flex-1 bg-sp-darkgray text-sp-subtext text-xs px-3 py-2 rounded font-mono truncate">
                      {connectionKey}
                    </code>
                    <button
                      onClick={() => navigator.clipboard.writeText(connectionKey)}
                      className="text-sp-subtext hover:text-sp-white text-xs px-3 py-2 bg-sp-lightgray rounded transition-colors"
                    >
                      Copy
                    </button>
                  </div>
                  <p className="text-sp-subtext text-xs mt-1">
                    Copy this now — it won't be shown again. Enter it on the in-store player to pair this location.
                  </p>
                </div>
              )}

              {channelError && <p className="text-red-400 text-sm">{channelError}</p>}

              {/* Channel selector */}
              <div>
                <label className="block text-sp-subtext text-sm mb-2">
                  Music channels{' '}
                  <span className="text-xs opacity-70">({assignedIds.size}/5 selected)</span>
                </label>
                <div className="space-y-2">
                  {channels.map(ch => {
                    const selected = assignedIds.has(ch.id);
                    const atLimit  = assignedIds.size >= 5 && !selected;
                    return (
                      <button
                        key={ch.id}
                        onClick={() => !atLimit && toggleChannel(ch.id)}
                        disabled={atLimit}
                        className={`w-full flex items-center gap-3 px-4 py-2.5 rounded-lg text-left transition-colors border ${
                          selected
                            ? 'bg-sp-green/15 border-sp-green/40'
                            : atLimit
                            ? 'bg-sp-darkgray border-transparent opacity-40 cursor-not-allowed'
                            : 'bg-sp-darkgray border-transparent hover:bg-sp-lightgray/20'
                        }`}
                      >
                        <span
                          className="w-3 h-3 rounded-full flex-shrink-0"
                          style={{ backgroundColor: ch.accentColor }}
                        />
                        <span className="text-sp-white text-sm font-medium flex-1">{ch.name}</span>
                        <span className="text-sp-subtext text-xs">{ch.description}</span>
                        {selected && <span className="text-sp-green text-sm">✓</span>}
                      </button>
                    );
                  })}
                </div>
              </div>
            </>
          )}
        </div>

        {/* Footer */}
        <div className="flex justify-end gap-3 px-6 py-4 border-t border-sp-lightgray/30">
          {step === 'info' ? (
            <>
              <button
                onClick={onClose}
                className="text-sp-subtext text-sm hover:text-sp-white transition-colors px-3"
              >
                Cancel
              </button>
              <button
                onClick={() => saveMutation.mutate()}
                disabled={!name.trim() || !location.trim() || saveMutation.isPending}
                className="bg-sp-green hover:bg-sp-green-hover text-sp-black font-bold px-5 py-2 rounded-full text-sm transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
              >
                {saveMutation.isPending ? 'Saving…' : isEdit ? 'Save Changes' : 'Continue →'}
              </button>
            </>
          ) : (
            <button
              onClick={onClose}
              className="bg-sp-green hover:bg-sp-green-hover text-sp-black font-bold px-5 py-2 rounded-full text-sm transition-colors"
            >
              Done
            </button>
          )}
        </div>

      </div>
    </div>
  );
}
