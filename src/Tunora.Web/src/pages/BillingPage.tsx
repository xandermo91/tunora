import { useState } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { billingApi } from '../api/billing';
import { parseApiError } from '../utils/errors';

const PLANS = [
  {
    tier: 'Starter',
    price: '$29',
    period: '/mo',
    description: 'Perfect for a single store.',
    features: ['1 location', '3 channels', 'Real-time playback', 'No scheduling'],
    accent: '#B3B3B3',
  },
  {
    tier: 'Professional',
    price: '$79',
    period: '/mo',
    description: 'For growing retail businesses.',
    features: ['5 locations', '5 channels per location', 'Real-time playback', 'Automated scheduling'],
    accent: '#1DB954',
    highlighted: true,
  },
  {
    tier: 'Business',
    price: '$149',
    period: '/mo',
    description: 'Multi-site operations.',
    features: ['20 locations', '5 channels per location', 'Real-time playback', 'Automated scheduling'],
    accent: '#FFB347',
  },
] as const;

const STATUS_COLOR: Record<string, string> = {
  Active:    'text-sp-green   bg-sp-green/15',
  Trialing:  'text-blue-400   bg-blue-900/20',
  PastDue:   'text-yellow-400 bg-yellow-900/20',
  Cancelled: 'text-red-400    bg-red-900/20',
};

export default function BillingPage() {
  const [billingError, setBillingError] = useState<string | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['billing-status'],
    queryFn: () => billingApi.getStatus().then(r => r.data),
  });

  const checkoutMutation = useMutation({
    mutationFn: (tier: string) =>
      billingApi.createCheckout(
        tier,
        `${window.location.origin}/billing?success=true`,
        `${window.location.origin}/billing`,
      ).then(r => r.data),
    onSuccess: ({ url }) => window.location.href = url,
    onMutate: () => setBillingError(null),
    onError: (err) => setBillingError(parseApiError(err, 'Failed to create checkout session.')),
  });

  const portalMutation = useMutation({
    mutationFn: () =>
      billingApi.createPortal(`${window.location.origin}/billing`).then(r => r.data),
    onSuccess: ({ url }) => window.location.href = url,
    onMutate: () => setBillingError(null),
    onError: (err) => setBillingError(parseApiError(err, 'Failed to open billing portal.')),
  });

  const searchParams = new URLSearchParams(window.location.search);
  const justUpgraded = searchParams.get('success') === 'true';

  const currentTier = data?.tier ?? 'Starter';

  return (
    <div className="p-8 max-w-4xl">
      <div className="mb-8">
        <h1 className="text-sp-white text-2xl font-bold">Billing</h1>
        <p className="text-sp-subtext text-sm mt-1">Manage your subscription and plan.</p>
      </div>

      {/* Error banner */}
      {billingError && (
        <div className="mb-6 bg-red-500/10 border border-red-500/30 rounded-lg px-5 py-3 text-red-400 text-sm flex items-center justify-between">
          {billingError}
          <button onClick={() => setBillingError(null)} className="ml-4 text-red-400/60 hover:text-red-400 transition-colors">✕</button>
        </div>
      )}

      {/* Success banner */}
      {justUpgraded && (
        <div className="mb-6 bg-sp-green/10 border border-sp-green/30 rounded-lg px-5 py-3 text-sp-green text-sm">
          Your plan has been upgraded successfully.
        </div>
      )}

      {/* Current plan */}
      {isLoading ? (
        <div className="text-sp-subtext text-sm py-8 text-center">Loading…</div>
      ) : data && (
        <div className="bg-sp-gray rounded-xl px-6 py-5 mb-8 flex items-center justify-between gap-4">
          <div>
            <p className="text-sp-subtext text-xs mb-1">Current plan</p>
            <div className="flex items-center gap-3">
              <span className="text-sp-white text-xl font-bold">{data.tier}</span>
              <span className={`text-xs font-medium px-2 py-0.5 rounded ${STATUS_COLOR[data.status] ?? STATUS_COLOR.Active}`}>
                {data.status}
              </span>
            </div>
            <p className="text-sp-subtext text-xs mt-1">
              {data.maxInstances === -1 ? 'Unlimited' : data.maxInstances} location{data.maxInstances !== 1 ? 's' : ''} ·{' '}
              {data.maxChannels} channels · {data.canSchedule ? 'Scheduling included' : 'No scheduling'}
            </p>
            {data.periodEnd && (
              <p className="text-sp-subtext text-xs mt-1">
                {data.status === 'Trialing' ? 'Trial ends' : 'Renews'}{' '}
                {new Date(data.periodEnd).toLocaleDateString()}
              </p>
            )}
          </div>
          {data.tier !== 'Starter' && (
            <button
              onClick={() => portalMutation.mutate()}
              disabled={portalMutation.isPending}
              className="flex-shrink-0 text-sm text-sp-subtext hover:text-sp-white border border-sp-lightgray/40 px-4 py-2 rounded-full transition-colors disabled:opacity-60"
            >
              {portalMutation.isPending ? 'Loading…' : 'Manage Subscription'}
            </button>
          )}
        </div>
      )}

      {/* Plan cards */}
      <h2 className="text-sp-white font-semibold text-sm mb-4">Plans</h2>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {PLANS.map(plan => {
          const isCurrent = currentTier === plan.tier;
          return (
            <div
              key={plan.tier}
              className={`rounded-xl px-5 py-5 flex flex-col ${'highlighted' in plan && plan.highlighted
                  ? 'bg-sp-green/10 border border-sp-green/30'
                  : 'bg-sp-gray'
              }`}
            >
              {/* Header */}
              <div className="mb-4">
                <div className="flex items-center justify-between mb-1">
                  <span className="text-sp-white font-bold text-sm">{plan.tier}</span>
                  {'highlighted' in plan && plan.highlighted && (
                    <span className="text-xs text-sp-green font-medium">Popular</span>
                  )}
                </div>
                <div className="flex items-baseline gap-0.5 mb-1">
                  <span className="text-sp-white text-2xl font-bold">{plan.price}</span>
                  <span className="text-sp-subtext text-sm">{plan.period}</span>
                </div>
                <p className="text-sp-subtext text-xs">{plan.description}</p>
              </div>

              {/* Features */}
              <ul className="space-y-1.5 flex-1 mb-5">
                {plan.features.map(f => (
                  <li key={f} className="flex items-center gap-2 text-xs text-sp-subtext">
                    <span style={{ color: plan.accent }}>✓</span>
                    {f}
                  </li>
                ))}
              </ul>

              {/* Action */}
              {isCurrent ? (
                <div className="text-center text-xs text-sp-subtext py-2 border border-sp-lightgray/30 rounded-full">
                  Current plan
                </div>
              ) : (
                <button
                  onClick={() => checkoutMutation.mutate(plan.tier)}
                  disabled={checkoutMutation.isPending}
                  className={`w-full py-2 rounded-full text-sm font-bold transition-colors disabled:opacity-60 ${'highlighted' in plan && plan.highlighted
                      ? 'bg-sp-green hover:bg-sp-green-hover text-sp-black'
                      : 'bg-sp-lightgray hover:bg-sp-lightgray/80 text-sp-white'
                  }`}
                >
                  {checkoutMutation.isPending ? 'Loading…' : 'Upgrade'}
                </button>
              )}
            </div>
          );
        })}
      </div>

      {/* Enterprise */}
      <div className="mt-4 bg-sp-gray rounded-xl px-5 py-4 flex items-center justify-between">
        <div>
          <span className="text-sp-white text-sm font-semibold">Enterprise</span>
          <p className="text-sp-subtext text-xs mt-0.5">Unlimited locations, custom SLAs, dedicated support.</p>
        </div>
        <a
          href="mailto:hello@tunora.io"
          className="flex-shrink-0 text-sm text-sp-subtext hover:text-sp-white transition-colors"
        >
          Contact us →
        </a>
      </div>
    </div>
  );
}
