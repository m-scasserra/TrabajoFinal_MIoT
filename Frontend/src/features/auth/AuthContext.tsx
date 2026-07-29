import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { login as apiLogin, logout as apiLogout, silentRefresh, type User } from '../../api/endpoints/auth';
import { apiFetch } from '../../api/client';

interface AuthState {
    user: User | null;
    loading: boolean;
    login: (email: string, password: string) => Promise<void>;
    logout: () => Promise<void>;
}

const AuthContext = createContext<AuthState | null>(null);

async function fetchMe(): Promise<User> {
    return apiFetch<User>('/auth/me');
}

export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<User | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        (async () => {
            const ok = await silentRefresh();
            if (ok) {
                try {
                    setUser(await fetchMe());
                } catch {
                    setUser(null);
                }
            }
            setLoading(false);
        })();
    }, []);

    const login = async (email: string, password: string) => {
        const u = await apiLogin(email, password);
        setUser(u);
    }

    const logout = async () => {
        await apiLogout();
        setUser(null);
    }

    return (
        <AuthContext.Provider value={{ user, loading, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
}

export function useAuth() {
    const ctx = useContext(AuthContext);
    if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
    return ctx;
}
