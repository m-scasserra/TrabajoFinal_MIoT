import { Navigate, Outlet, useLocation } from 'react-router';
import { useAuth } from '../features/auth/AuthContext';

export function ProtectedRoute() {
    const { user, loading } = useAuth();

    if (loading) return <div>Loading...</div>;

    if (!user) {
        const location = useLocation();
        return <Navigate to="/login" state={{ from: location.pathname }} replace />;
    }

    return <Outlet />;
}