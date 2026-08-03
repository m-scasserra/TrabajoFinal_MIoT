import { useState } from "react";
import { Outlet, useNavigate, NavLink } from "react-router";
import { useAuth } from "../features/auth/AuthContext";

export function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [loggingOut, setLoggingOut] = useState(false);

  const handleLogout = async () => {
    setLoggingOut(true);
    try {
      await logout();
      navigate("/login", { replace: true });
    } catch {
      navigate("/login", { replace: true });
    }
  };

  return (
    <div className="min-h-screen flex flex-col bg_gray-50">
      <header className="bg-white border-b border-gray-200">
        <div className="max-w-6xl mx-auto px-4 h-14 flex items-center justify-between">
          <nav className="flex items-center gap-1">
            <NavLink
              to="/"
              end
              className={({ isActive }) =>
                `px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                  isActive
                    ? "bg-gray-100 text-gray-900"
                    : "text-gray-600 hover:text-gray-900"
                }`
              }
            >
              Home
            </NavLink>
            <NavLink
              to="/users"
              className={({ isActive }) =>
                `px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                  isActive
                    ? "bg-gray-100 text-gray-900"
                    : "text-gray-600 hover:text-gray-900"
                }`
              }
            >
              Users
            </NavLink>
          </nav>

          <NavLink
            to="/organisations/new"
            className={({ isActive }) =>
              `px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                isActive
                  ? "bg-gray-100 text-gray-900"
                  : "text-gray-600 hover:text-gray-900"
              }`
            }
          >
            Create Organisation
          </NavLink>

          <div className="flex items-center gap-3">
            {user && (
              <div className="text-right leading-tight hidden sm:block">
                <div className="text-sm font-medium text-gray-900">
                  {user.email}
                </div>
                <div className="text-xs text-gray-500">{user.role}</div>
              </div>
            )}
            <button
              onClick={handleLogout}
              disabled={loggingOut}
              className="rounded-lg border border-gray-300 bg-white px-3 py-1.5 text-sm
                                       font-medium text-gray-700 hover:bg-gray-50 focus:outline-none
                                       focus:ring-2 focus:ring-blue-500 focus:ring-offset-1
                                       disabled:opacity-60 disabled:cursor-not-allowed transition-colors"
            >
              {loggingOut ? "Logging out..." : "Logout"}
            </button>
          </div>
        </div>
      </header>

      <main className="flex-1">
        <div className="max-w-6xl mx-auto px-4 py-6">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
