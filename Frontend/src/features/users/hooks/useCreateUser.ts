import { useState } from 'react';
import { createUser, type CreateUserPayload } from '../../../api/endpoints/users';
import { ApiError } from '../../../api/client';

interface UseCreateUserResult {
    submit: (payload: CreateUserPayload) => Promise<boolean>;
    submitting: boolean;
    error: string | null;
}

export function useCreateUser(): UseCreateUserResult {
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const submit = async (payload: CreateUserPayload): Promise<boolean> => {
        setSubmitting(true);
        setError(null);
        try {
            await createUser(payload);
            return true;
        } catch (err) {
            if (err instanceof ApiError) {
                setError(err.message);
            } else {
                setError('Could not create user.');
            }
            return false;
        } finally {
            setSubmitting(false);
        }
    };

    return { submit, submitting, error };
}