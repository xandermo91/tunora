import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { advisorApi, type AdvisorResponse } from '../api/advisor';
import { parseApiError } from '../utils/errors';

type Tab = 'music' | 'analytics';

export default function AIAdvisorPanel() {
  const [open, setOpen] = useState(false);
  const [tab, setTab] = useState<Tab>('music');
  const [businessType, setBusinessType] = useState('');
  const [description, setDescription] = useState('');
  const [result, setResult] = useState<AdvisorResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const musicMutation = useMutation({
    mutationFn: () => advisorApi.getMusicAdvice({ businessType, description: description || undefined }),
    onSuccess: (data) => { setResult(data); setError(null); },
    onError: (err) => { setError(parseApiError(err, 'AI advisor is unavailable. Please try again later.')); setResult(null); },
  });

  const analyticsMutation = useMutation({
    mutationFn: () => advisorApi.getAnalyticsInsight(),
    onSuccess: (data) => { setResult(data); setError(null); },
    onError: (err) => { setError(parseApiError(err, 'AI advisor is unavailable. Please try again later.')); setResult(null); },
  });

  const isPending = musicMutation.isPending || analyticsMutation.isPending;

  const handleTabChange = (t: Tab) => {
    setTab(t);
    setResult(null);
    setError(null);
  };

  return (
    <div className="bg-sp-gray rounded-xl px-6 py-5 mt-6">
      {/* Header */}
      <button
        onClick={() => setOpen(o => !o)}
        className="w-full flex items-center justify-between text-left"
      >
        <div className="flex items-center gap-2">
          <span className="text-sp-green text-sm">✦</span>
          <span className="text-sp-white font-semibold text-sm">AI Music Advisor</span>
          <span className="text-xs text-sp-subtext bg-sp-lightgray/30 px-2 py-0.5 rounded">Beta</span>
        </div>
        <span className="text-sp-subtext text-xs">{open ? '▲' : '▼'}</span>
      </button>

      {open && (
        <div className="mt-4">
          {/* Tabs */}
          <div className="flex gap-1 mb-4 bg-sp-darkgray rounded-lg p-1">
            {(['music', 'analytics'] as Tab[]).map(t => (
              <button
                key={t}
                onClick={() => handleTabChange(t)}
                className={`flex-1 py-1.5 rounded text-xs font-medium transition-colors ${
                  tab === t ? 'bg-sp-lightgray text-sp-white' : 'text-sp-subtext hover:text-sp-white'
                }`}
              >
                {t === 'music' ? 'Music Advisor' : 'Analytics Insight'}
              </button>
            ))}
          </div>

          {tab === 'music' ? (
            <div className="space-y-3">
              <div>
                <label className="block text-sp-subtext text-xs mb-1">Business type</label>
                <input
                  type="text"
                  value={businessType}
                  onChange={e => setBusinessType(e.target.value)}
                  placeholder="e.g. café, gym, retail store"
                  maxLength={200}
                  className="w-full bg-sp-darkgray border border-sp-lightgray/40 rounded px-3 py-2 text-sp-white text-xs focus:outline-none focus:border-sp-green/50 placeholder-sp-subtext"
                />
              </div>
              <div>
                <label className="block text-sp-subtext text-xs mb-1">Additional context (optional)</label>
                <input
                  type="text"
                  value={description}
                  onChange={e => setDescription(e.target.value)}
                  placeholder="e.g. upscale, family-friendly, evening crowd"
                  maxLength={500}
                  className="w-full bg-sp-darkgray border border-sp-lightgray/40 rounded px-3 py-2 text-sp-white text-xs focus:outline-none focus:border-sp-green/50 placeholder-sp-subtext"
                />
              </div>
              <button
                onClick={() => musicMutation.mutate()}
                disabled={isPending || !businessType.trim()}
                className="bg-sp-green hover:bg-sp-green-hover text-sp-black font-bold px-4 py-2 rounded-full text-xs transition-colors disabled:opacity-60"
              >
                {isPending ? 'Thinking…' : 'Get Recommendations'}
              </button>
            </div>
          ) : (
            <div>
              <p className="text-sp-subtext text-xs mb-3">
                Get AI-powered insight based on your current location and scheduling data.
              </p>
              <button
                onClick={() => analyticsMutation.mutate()}
                disabled={isPending}
                className="bg-sp-green hover:bg-sp-green-hover text-sp-black font-bold px-4 py-2 rounded-full text-xs transition-colors disabled:opacity-60"
              >
                {isPending ? 'Thinking…' : 'Analyse My Data'}
              </button>
            </div>
          )}

          {/* Error state */}
          {error && (
            <p className="mt-3 text-red-400 text-xs">{error}</p>
          )}

          {/* Result */}
          {result && (
            <div className="mt-4 space-y-3">
              <p className="text-sp-white text-sm leading-relaxed">{result.insight}</p>
              {result.suggestions.length > 0 && (
                <ul className="space-y-1.5">
                  {result.suggestions.map((s, i) => (
                    <li key={i} className="flex items-start gap-2 text-sp-subtext text-xs">
                      <span className="text-sp-green mt-0.5 flex-shrink-0">→</span>
                      {s}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
