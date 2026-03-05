import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useAuthStore } from '../store/authStore';
import { analyticsApi } from '../api/analytics';
import { instancesApi } from '../api/instances';
import type { Instance } from '../types/instances';

const STATUS_STYLE: Record<Instance['status'], string> = {
  Offline:  'text-sp-subtext  bg-sp-lightgray/50',
  Online:   'text-blue-400    bg-blue-900/30',
  Playing:  'text-sp-green    bg-sp-green/20',
  Stopped:  'text-yellow-400  bg-yellow-900/30',
};

function StatCard({ label, value, sub, loading }: { label: string; value: string | number; sub?: string; loading?: boolean }) {
  return (
    <div className="bg-sp-gray rounded-lg p-5">
      <p className="text-sp-subtext text-sm mb-1">{label}</p>
      {loading ? (
        <div className="h-9 w-16 bg-sp-lightgray/40 rounded animate-pulse mt-1" />
      ) : (
        <p className="text-sp-white text-3xl font-bold">{value}</p>
      )}
      {sub && <p className="text-sp-subtext text-xs mt-1">{sub}</p>}
    </div>
  );
}

export default function OverviewPage() {
  const user = useAuthStore((s) => s.user);

  const { data: stats, isLoading: statsLoading } = useQuery({
    queryKey: ['analytics-overview'],
    queryFn: () => analyticsApi.getOverview().then(r => r.data),
    refetchInterval: 15_000,
  });

  const { data: instances = [], isLoading: instancesLoading } = useQuery({
    queryKey: ['instances'],
    queryFn: () => instancesApi.list().then(r => r.data),
    refetchInterval: 15_000,
  });

  return (
    <div className="p-8">
      <div className="mb-8">
        <h1 className="text-sp-white text-2xl font-bold">
          Welcome back{user?.firstName ? `, ${user.firstName}` : ''}
        </h1>
        <p className="text-sp-subtext text-sm mt-1">Here&apos;s what&apos;s happening across your locations.</p>
      </div>

      {/* Stats row */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
        <StatCard
          label="Active Locations"
          value={stats?.activeLocations ?? 0}
          sub={`${stats?.playingNow ?? 0} playing now`}
          loading={statsLoading}
        />
        <StatCard
          label="Total Locations"
          value={stats?.totalLocations ?? 0}
          sub="Across all sites"
          loading={statsLoading}
        />
        <StatCard
          label="Scheduled Events"
          value={stats?.schedulesThisWeek ?? 0}
          sub="This week"
          loading={statsLoading}
        />
      </div>

      {/* Instance grid */}
      <div>
        <h2 className="text-sp-white text-lg font-semibold mb-4">Locations</h2>

        {instancesLoading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {[1, 2, 3].map(i => (
              <div key={i} className="bg-sp-gray rounded-lg p-5 animate-pulse">
                <div className="h-4 w-32 bg-sp-lightgray/40 rounded mb-2" />
                <div className="h-3 w-20 bg-sp-lightgray/30 rounded mb-4" />
                <div className="h-3 w-24 bg-sp-lightgray/20 rounded" />
              </div>
            ))}
          </div>
        ) : instances.length === 0 ? (
          <div className="bg-sp-gray rounded-lg p-12 flex flex-col items-center justify-center text-center">
            <div className="w-12 h-12 rounded-full bg-sp-lightgray flex items-center justify-center mb-4">
              <span className="text-2xl">📍</span>
            </div>
            <p className="text-sp-white font-medium mb-1">No locations yet</p>
            <p className="text-sp-subtext text-sm">
              Go to{' '}
              <Link to="/instances" className="text-sp-green hover:underline">
                Locations
              </Link>{' '}
              to add your first store.
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {instances.map(inst => (
              <Link
                key={inst.id}
                to={`/instances/${inst.id}`}
                className="bg-sp-gray rounded-lg p-5 hover:bg-sp-lightgray/30 transition-colors block"
              >
                <div className="flex items-start justify-between mb-3">
                  <div>
                    <p className="text-sp-white font-semibold text-sm">{inst.name}</p>
                    <p className="text-sp-subtext text-xs mt-0.5">{inst.location}</p>
                  </div>
                  <span className={`text-xs font-medium px-2 py-0.5 rounded ${STATUS_STYLE[inst.status]}`}>
                    {inst.status}
                  </span>
                </div>

                {inst.status === 'Playing' && inst.currentTrackTitle ? (
                  <div className="border-t border-sp-lightgray/20 pt-3">
                    <p className="text-sp-green text-xs font-medium truncate">{inst.currentTrackTitle}</p>
                    <p className="text-sp-subtext text-xs truncate">{inst.currentTrackArtist}</p>
                  </div>
                ) : (
                  <div className="border-t border-sp-lightgray/20 pt-3">
                    <p className="text-sp-subtext text-xs">
                      {inst.channels.length > 0
                        ? `${inst.channels.length} channel${inst.channels.length !== 1 ? 's' : ''} assigned`
                        : 'No channels assigned'}
                    </p>
                  </div>
                )}
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
