import { NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import { authApi } from '../../api/auth';

const navItems = [
  { to: '/dashboard',  label: 'Overview',  icon: '⊞' },
  { to: '/instances',  label: 'Locations', icon: '📍' },
  { to: '/billing',    label: 'Billing',   icon: '💳' },
  { to: '/settings',   label: 'Settings',  icon: '⚙' },
];

export default function Sidebar() {
  const { user, clearAuth } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = async () => {
    try { await authApi.logout(); } catch { /* ignore */ }
    clearAuth();
    navigate('/login');
  };

  return (
    <aside className="w-60 min-h-screen bg-sp-black flex flex-col flex-shrink-0">
      {/* Logo */}
      <div className="px-6 py-8">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 bg-sp-green rounded-full flex items-center justify-center text-sp-black font-bold text-sm">
            T
          </div>
          <span className="text-sp-white font-bold text-lg tracking-tight">Tunora</span>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-3">
        <ul className="space-y-1">
          {navItems.map(({ to, label, icon }) => (
            <li key={to}>
              <NavLink
                to={to}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium transition-colors
                   ${isActive
                     ? 'bg-sp-gray text-sp-white'
                     : 'text-sp-subtext hover:text-sp-white hover:bg-sp-gray/50'
                   }`
                }
              >
                <span className="text-base w-5 text-center">{icon}</span>
                {label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      {/* User footer */}
      <div className="px-4 py-4 border-t border-sp-lightgray/30">
        <div className="flex items-center gap-3 mb-3">
          <div className="w-8 h-8 rounded-full bg-sp-green flex items-center justify-center text-sp-black text-sm font-bold">
            {user?.firstName?.[0] ?? user?.email?.[0]?.toUpperCase() ?? 'U'}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sp-white text-sm font-medium truncate">
              {user?.firstName ? `${user.firstName} ${user.lastName}` : user?.email}
            </p>
            <p className="text-sp-subtext text-xs truncate">{user?.role}</p>
          </div>
        </div>
        <button
          onClick={handleLogout}
          className="w-full text-left text-sp-subtext text-xs hover:text-sp-white transition-colors px-1"
        >
          Log out
        </button>
      </div>
    </aside>
  );
}
