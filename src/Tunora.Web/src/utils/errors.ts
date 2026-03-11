export function parseApiError(err: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (typeof err === 'object' && err !== null) {
    const r = (err as { response?: { data?: { error?: string } } }).response;
    if (r?.data?.error) return r.data.error;
  }
  return fallback;
}
