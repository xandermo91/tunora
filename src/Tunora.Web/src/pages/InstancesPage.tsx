import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { instancesApi } from '../api/instances';
import InstanceModal from '../components/instances/InstanceModal';
import { parseApiError } from '../utils/errors';
import type { Instance } from '../types/instances';

const STATUS_STYLE: Record<string, string> = {
  Offline:  'text-sp-subtext  bg-sp-lightgray/50',
  Online:   'text-blue-400    bg-blue-900/30',
  Playing:  'text-sp-green    bg-sp-green/20',
  Stopped:  'text-yellow-400  bg-yellow-900/30',
};

export default function InstancesPage() {
  const qc = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Instance | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const { data: instances = [], isLoading } = useQuery({
    queryKey: ['instances'],
    queryFn: () => instancesApi.list().then(r => r.data),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => instancesApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['instances'] }),
    onError: (err) => setDeleteError(parseApiError(err, 'Failed to delete location.')),
  });

  const openAdd  = () => { setEditing(null); setModalOpen(true); };
  const openEdit = (inst: Instance) => { setEditing(inst); setModalOpen(true); };
  const closeModal = () => { setModalOpen(false); setEditing(null); };

  return (
    <div className="p-8">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-sp-white text-2xl font-bold">Locations</h1>
          <p className="text-sp-subtext text-sm mt-1">Manage your in-store music players.</p>
        </div>
        <button
          onClick={openAdd}
          className="bg-sp-green hover:bg-sp-green-hover text-sp-black font-bold px-4 py-2 rounded-full text-sm transition-colors"
        >
          + Add Location
        </button>
      </div>

      {deleteError && (
        <div className="mb-4 bg-red-500/10 border border-red-500/30 rounded-lg px-5 py-3 text-red-400 text-sm flex items-center justify-between">
          {deleteError}
          <button onClick={() => setDeleteError(null)} className="ml-4 text-red-400/60 hover:text-red-400 transition-colors">✕</button>
        </div>
      )}

      {isLoading ? (
        <div className="text-sp-subtext text-center py-16">Loading…</div>
      ) : instances.length === 0 ? (
        <div className="bg-sp-gray rounded-lg p-16 flex flex-col items-center justify-center text-center">
          <div className="w-12 h-12 rounded-full bg-sp-lightgray flex items-center justify-center mb-4 text-2xl">📍</div>
          <p className="text-sp-white font-medium mb-1">No locations yet</p>
          <p className="text-sp-subtext text-sm mb-4">Add your first store location to get started.</p>
          <button
            onClick={openAdd}
            className="bg-sp-green hover:bg-sp-green-hover text-sp-black font-bold px-5 py-2 rounded-full text-sm transition-colors"
          >
            Add Location
          </button>
        </div>
      ) : (
        <div className="bg-sp-gray rounded-lg overflow-hidden">
          <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-sp-lightgray/40">
                {['Name', 'Address', 'Status', 'Channels', ''].map(h => (
                  <th key={h} className="text-left text-sp-subtext text-xs font-medium px-5 py-3 uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-sp-lightgray/20">
              {instances.map(inst => (
                <tr key={inst.id} className="hover:bg-sp-lightgray/10 transition-colors">
                  <td className="px-5 py-4">
                    <Link
                      to={`/instances/${inst.id}`}
                      className="text-sp-white text-sm font-medium hover:text-sp-green transition-colors"
                    >
                      {inst.name}
                    </Link>
                  </td>
                  <td className="px-5 py-4 text-sp-subtext text-sm">{inst.location}</td>
                  <td className="px-5 py-4">
                    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${STATUS_STYLE[inst.status] ?? STATUS_STYLE.Offline}`}>
                      {inst.status}
                    </span>
                  </td>
                  <td className="px-5 py-4">
                    {inst.channels.length === 0 ? (
                      <span className="text-sp-subtext text-xs">None assigned</span>
                    ) : (
                      <div className="flex items-center gap-1.5 flex-wrap">
                        {inst.channels.map(ch => (
                          <span
                            key={ch.channelId}
                            className="w-2.5 h-2.5 rounded-full"
                            style={{ backgroundColor: ch.accentColor }}
                            title={ch.name}
                          />
                        ))}
                        <span className="text-sp-subtext text-xs ml-1">{inst.channels.length}</span>
                      </div>
                    )}
                  </td>
                  <td className="px-5 py-4">
                    <div className="flex items-center gap-4 justify-end">
                      <Link
                        to={`/instances/${inst.id}`}
                        className="text-sp-green hover:text-sp-green-hover text-xs font-medium transition-colors"
                      >
                        Manage ▶
                      </Link>
                      <button
                        onClick={() => openEdit(inst)}
                        className="text-sp-subtext hover:text-sp-white text-xs transition-colors"
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => {
                          if (window.confirm(`Delete "${inst.name}"? This cannot be undone.`)) {
                            setDeleteError(null);
                            deleteMutation.mutate(inst.id);
                          }
                        }}
                        disabled={deleteMutation.isPending}
                        className="text-sp-subtext hover:text-red-400 text-xs transition-colors disabled:opacity-50"
                      >
                        {deleteMutation.isPending ? '…' : 'Delete'}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
        </div>
      )}

      {modalOpen && <InstanceModal instance={editing} onClose={closeModal} />}
    </div>
  );
}
