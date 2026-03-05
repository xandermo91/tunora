import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { authApi } from '../api/auth';
import { useAuthStore } from '../store/authStore';

export default function RegisterPage() {
  const navigate = useNavigate();
  const setTokens = useAuthStore((s) => s.setTokens);

  const [form, setForm] = useState({
    companyName: '',
    email: '',
    password: '',
    firstName: '',
    lastName: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const set = (field: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((prev) => ({ ...prev, [field]: e.target.value }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const { data } = await authApi.register(form);
      setTokens(data.accessToken, data.refreshToken);
      navigate('/dashboard');
    } catch (err: unknown) {
      const msg =
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      setError(msg ?? 'Registration failed. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-sp-black flex items-center justify-center px-4 py-8">
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
          <h1 className="text-sp-white text-xl font-bold mb-6 text-center">Create your account</h1>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sp-subtext text-sm mb-1.5" htmlFor="companyName">
                Company name
              </label>
              <input
                id="companyName"
                type="text"
                required
                maxLength={200}
                value={form.companyName}
                onChange={set('companyName')}
                className="w-full bg-sp-lightgray text-sp-white rounded px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-sp-green placeholder-sp-subtext"
                placeholder="Acme Supermarkets"
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-sp-subtext text-sm mb-1.5" htmlFor="firstName">
                  First name
                </label>
                <input
                  id="firstName"
                  type="text"
                  required
                  maxLength={100}
                  value={form.firstName}
                  onChange={set('firstName')}
                  className="w-full bg-sp-lightgray text-sp-white rounded px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-sp-green placeholder-sp-subtext"
                  placeholder="Jane"
                />
              </div>
              <div>
                <label className="block text-sp-subtext text-sm mb-1.5" htmlFor="lastName">
                  Last name
                </label>
                <input
                  id="lastName"
                  type="text"
                  required
                  maxLength={100}
                  value={form.lastName}
                  onChange={set('lastName')}
                  className="w-full bg-sp-lightgray text-sp-white rounded px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-sp-green placeholder-sp-subtext"
                  placeholder="Smith"
                />
              </div>
            </div>

            <div>
              <label className="block text-sp-subtext text-sm mb-1.5" htmlFor="email">
                Work email
              </label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                required
                maxLength={256}
                value={form.email}
                onChange={set('email')}
                className="w-full bg-sp-lightgray text-sp-white rounded px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-sp-green placeholder-sp-subtext"
                placeholder="jane@acme.com"
              />
            </div>

            <div>
              <label className="block text-sp-subtext text-sm mb-1.5" htmlFor="password">
                Password
              </label>
              <input
                id="password"
                type="password"
                autoComplete="new-password"
                required
                minLength={8}
                maxLength={128}
                value={form.password}
                onChange={set('password')}
                className="w-full bg-sp-lightgray text-sp-white rounded px-3 py-2.5 text-sm outline-none focus:ring-2 focus:ring-sp-green placeholder-sp-subtext"
                placeholder="Min. 8 characters"
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
              {loading ? 'Creating account…' : 'Get started free'}
            </button>
          </form>
        </div>

        <p className="text-sp-subtext text-sm text-center mt-6">
          Already have an account?{' '}
          <Link to="/login" className="text-sp-white hover:text-sp-green transition-colors">
            Log in
          </Link>
        </p>
      </div>
    </div>
  );
}
