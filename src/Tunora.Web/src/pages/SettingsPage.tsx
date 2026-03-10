import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useAuthStore } from '../store/authStore';
import { authApi } from '../api/auth';
import type { AxiosError } from 'axios';

export default function SettingsPage() {
  const user = useAuthStore((s) => s.user);

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword]         = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [validationError, setValidationError] = useState('');
  const [successMessage, setSuccessMessage]   = useState('');

  const changePwMutation = useMutation({
    mutationFn: (data: { currentPassword: string; newPassword: string }) =>
      authApi.changePassword(data),
    onSuccess: () => {
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
      setValidationError('');
      setSuccessMessage('Password changed successfully.');
      setTimeout(() => setSuccessMessage(''), 4000);
    },
    onError: (err: AxiosError<{ error: string }>) => {
      setValidationError(err.response?.data?.error ?? 'Something went wrong.');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setValidationError('');
    setSuccessMessage('');

    if (newPassword.length < 8) {
      setValidationError('New password must be at least 8 characters.');
      return;
    }
    if (newPassword !== confirmPassword) {
      setValidationError('New passwords do not match.');
      return;
    }

    changePwMutation.mutate({ currentPassword, newPassword });
  };

  return (
    <div className="p-8 max-w-2xl">
      <div className="mb-8">
        <h1 className="text-sp-white text-2xl font-bold">Settings</h1>
        <p className="text-sp-subtext text-sm mt-1">Manage your account preferences.</p>
      </div>

      {/* Profile info */}
      <section className="bg-sp-gray rounded-xl px-6 py-5 mb-6">
        <h2 className="text-sp-white font-semibold text-sm mb-4">Profile</h2>
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-sp-subtext text-sm">Name</span>
            <span className="text-sp-white text-sm">
              {user?.firstName} {user?.lastName}
            </span>
          </div>
          <div className="flex items-center justify-between border-t border-sp-lightgray/20 pt-3">
            <span className="text-sp-subtext text-sm">Email</span>
            <span className="text-sp-white text-sm">{user?.email}</span>
          </div>
          <div className="flex items-center justify-between border-t border-sp-lightgray/20 pt-3">
            <span className="text-sp-subtext text-sm">Role</span>
            <span className="text-sp-white text-sm">{user?.role}</span>
          </div>
        </div>
      </section>

      {/* Change password */}
      <section className="bg-sp-gray rounded-xl px-6 py-5">
        <h2 className="text-sp-white font-semibold text-sm mb-4">Change Password</h2>

        {successMessage && (
          <div className="mb-4 bg-sp-green/10 border border-sp-green/30 rounded-lg px-4 py-2.5 text-sp-green text-sm">
            {successMessage}
          </div>
        )}
        {validationError && (
          <div className="mb-4 bg-red-900/20 border border-red-700/40 rounded-lg px-4 py-2.5 text-red-400 text-sm">
            {validationError}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sp-subtext text-xs mb-1.5">Current password</label>
            <input
              type="password"
              value={currentPassword}
              onChange={e => setCurrentPassword(e.target.value)}
              required
              className="w-full bg-sp-black border border-sp-lightgray/40 rounded-lg px-3 py-2 text-sp-white text-sm focus:outline-none focus:border-sp-green transition-colors"
            />
          </div>
          <div>
            <label className="block text-sp-subtext text-xs mb-1.5">New password</label>
            <input
              type="password"
              value={newPassword}
              onChange={e => setNewPassword(e.target.value)}
              required
              minLength={8}
              className="w-full bg-sp-black border border-sp-lightgray/40 rounded-lg px-3 py-2 text-sp-white text-sm focus:outline-none focus:border-sp-green transition-colors"
            />
          </div>
          <div>
            <label className="block text-sp-subtext text-xs mb-1.5">Confirm new password</label>
            <input
              type="password"
              value={confirmPassword}
              onChange={e => setConfirmPassword(e.target.value)}
              required
              className="w-full bg-sp-black border border-sp-lightgray/40 rounded-lg px-3 py-2 text-sp-white text-sm focus:outline-none focus:border-sp-green transition-colors"
            />
          </div>
          <div className="pt-1">
            <button
              type="submit"
              disabled={changePwMutation.isPending}
              className="bg-sp-green hover:bg-sp-green-hover text-sp-black font-bold px-5 py-2 rounded-full text-sm transition-colors disabled:opacity-60"
            >
              {changePwMutation.isPending ? 'Saving…' : 'Update Password'}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
