import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { authApi } from '../api/auth';
import { useAuthStore } from '../store/authStore';

export default function LoginPage() {
  const navigate = useNavigate();
  const setTokens = useAuthStore((s) => s.setTokens);

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const { data } = await authApi.login({ email, password });
      setTokens(data.accessToken, data.refreshToken);
      navigate('/dashboard');
    } catch (err: unknown) {
      const msg =
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      setError(msg ?? 'Invalid email or password.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-sp-black flex items-center justify-center px-4">
      <div className="w-full max-w-sm">
        {/* Logo */}
        <div className="flex justify-center mb-8">
          <div className="flex items-center gap-2">
            <div className="w-10 h-10 bg-sp-green rounded-full flex items-center justify-center text-sp-black font-bold text-lg">
              M
            </div>
            <span className="text-sp-white font-bold text-2xl tracking-tight">Tunora</span>
          </div>
        </div>

        <div className="bg-sp-gray rounded-lg px-8 py-10">
          <h1 className="text-sp-white text-xl font-bold mb-6 text-center">Log in to your account</h1>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sp-subtext text-sm mb-1.5" htmlFor="email">
                Email
              </label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full bg-sp-lightgray text-sp-white rounded px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-sp-green placeholder-sp-subtext"
                placeholder="you@company.com"
              />
            </div>

            <div>
              <label className="block text-sp-subtext text-sm mb-1.5" htmlFor="password">
                Password
              </label>
              <input
                id="password"
                type="password"
                autoComplete="current-password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full bg-sp-lightgray text-sp-white rounded px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-sp-green placeholder-sp-subtext"
                placeholder="••••••••"
              />
            </div>

            {error && (
              <p className="text-red-400 text-sm text-center">{error}</p>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-sp-green hover:bg-sp-green-hover text-sp-black font-bold py-2.5 rounded-full text-sm transition-colors disabled:opacity-60 disabled:cursor-not-allowed mt-2"
            >
              {loading ? 'Logging in…' : 'Log In'}
            </button>
          </form>
        </div>

        <p className="text-sp-subtext text-sm text-center mt-6">
          Don&apos;t have an account?{' '}
          <Link to="/register" className="text-sp-white hover:text-sp-green transition-colors">
            Sign up
          </Link>
        </p>
      </div>
    </div>
  );
}
