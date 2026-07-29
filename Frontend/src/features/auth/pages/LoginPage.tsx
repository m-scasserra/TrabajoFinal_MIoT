import { useState, type FormEvent } from 'react';
import { useNavigate, useLocation, Navigate } from 'react-router';
import { useAuth } from '../AuthContext';
import { ApiError } from '../../../api/client';

export function LoginPage() {
    const { user, loading, login } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();

    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);

    const from = (location.state as { from?: string })?.from ?? '/';

    if (loading) return <div>Loading...</div>;
    if (user) return <Navigate to={from} replace />;

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError(null);
        setSubmitting(true);

        try {
            await login(email, password);
            navigate(from, { replace: true });
        } catch (err) {
            if (err instanceof ApiError && err.status === 401) {
                setError('Invalid email or password.');
            } else {
                setError('An unexpected error occurred. Please try again.');
            }
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
            <div className="w-full max-w-sm bg-white rounded-xl shadow-sm border border-gray-200 p-8">
                <h1 className="text-2xl font-semibold text-gray-900 mb-6">Login</h1>

                <form onSubmit={handleSubmit} className="space-y-4" noValidate>
                    <div>
                        <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                            Email
                        </label>
                        <input
                            id="email"
                            type="email"
                            autoComplete="email"
                            required
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            disabled={submitting}
                            className="w-full rounded-lg border border-gray-300 px-3 text-sm
                                       focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                                       disabled:bg-gray-100"
                        />
                    </div>

                    <div>
                        <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
                            Password
                        </label>
                        <input
                            id="password"
                            type="password"
                            autoComplete="current-password"
                            required
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            disabled={submitting}
                            className="w-full rounded-lg border border-gray-300 px-3 text-sm
                                       focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                                       disabled:bg-gray-100"
                        />
                    </div>

                    {error && (
                        <div
                            role="alert"
                            className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2"
                        >
                            {error}
                        </div>
                    )}

                    <button
                        type="submit"
                        disabled={submitting}
                        className="w-full rounded-lg bg-blue-600 text-white text-sm font-medium py-2.5
                                   hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500
                                   focus:ring-offset-2 disabled:opacity-60 disabled:cursor-not-allowed
                                   transition-colors"
                    >
                        {submitting ? 'Logging in...' : 'Login'}
                    </button>
                </form>
            </div>
        </div>
    );
}