import { apiFetch, tokenStore } from '../client';

export interface User {
    id: string;
    username: string;
    role: string;
}

interface AuthResponse {
    accessToken: string;
    user: User;
}

export async function login(username: string, password: string): Promise<User> {
    const data = await apiFetch<AuthResponse>('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ username, password }),
        skipAuthRetry: true,
    });

    tokenStore.set(data.accessToken);
    return data.user;
}

export async function logout(): Promise<void> {
    await apiFetch('/auth/logout', { method: 'POST', skipAuthRetry: true });
    tokenStore.set(null);
}

export async function silentRefresh(): Promise<boolean> {
    try {
        const data = await apiFetch<{ accessToken: string }>('/auth/refresh', {
            method: 'POST',
            skipAuthRetry: true,
        });
        tokenStore.set(data.accessToken);
        return true;
    } catch {
        return false;
    }
}

export async function confirmAccount(token: string, password: string): Promise<void> {
    await apiFetch('/auth/confirm', {
        method: 'POST',
        body: JSON.stringify({ token, password }),
        skipAuthRetry: true,
    });
}