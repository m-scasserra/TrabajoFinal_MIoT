const BASE_URL = '/api/v1';

let accessToken: string | null = null;

export const tokenStore = {
    get: () => accessToken,
    set: (t: string | null) => { accessToken = t; },
};

let refreshPromise: Promise<boolean> | null = null;

async function doRefresh(): Promise<boolean> {
    if (refreshPromise) return refreshPromise;

    refreshPromise = (async () => {
        try {
            const res = await fetch(`${BASE_URL}/auth/refresh`, {
                method: 'POST',
                credentials: 'include',
            });
            if (!res.ok) {
                tokenStore.set(null);
                return false
            }
            const data = await res.json();
            tokenStore.set(data.accessToken);
            return true;
        }
        catch {
            tokenStore.set(null);
            return false;
        } finally {
            refreshPromise = null;
        }
    })();

    return refreshPromise;
}

interface ApiOptions extends RequestInit {
    skipAuthRetry?: boolean;
}

export async function apiFetch<T = unknown>(
    path: string,
    options: ApiOptions = {},
): Promise<T> {
    const { skipAuthRetry, headers, ...rest } = options;

    const makeRequest = () => {
        const token = tokenStore.get();
        return fetch(`${BASE_URL}${path}`, {
            ...rest,
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json',
                ...(token ? { Authorization: `Bearer ${token}` } : {}),
                ...headers,
            },
        });
    };

    let res = await makeRequest();

    if (res.status === 401 && !skipAuthRetry) {
        const refreshed = await doRefresh();
        if (refreshed) {
            res = await makeRequest();
        } else {
            throw new ApiError(401, 'Unauthorized');
        }
    }

    if (!res.ok) {
        let message = `Error ${res.status}`;
        try {
            const body = await res.json();
            if (body?.message) message = body.message;
        } catch { }
        throw new ApiError(res.status, message);
    }

    if (res.status === 204) return undefined as T;
    return res.json() as Promise<T>;
}

export class ApiError extends Error {
    public status: number;

    constructor(status: number, message: string) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
    }
}