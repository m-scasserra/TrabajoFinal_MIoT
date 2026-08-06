import { useState, type FormEvent } from "react";
import { useSearchParams, useNavigate, Link } from "react-router";
import { confirmAccount } from "../../../api/endpoints/auth";
import { ApiError } from "../../../api/client";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/FormField";

const MIN_PASSWORD = 8;

export function ConfirmAccountPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get("token");

  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [done, setDone] = useState(false);

  if (!token) {
    return (
      <CenteredCard>
        <CardHeader>
          <CardTitle>Invalid Confirmation Link</CardTitle>
          <CardDescription>
            The confirmation link is invalid or incomplete.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button asChild variant="outline" className="w-full">
            <Link to="/login">Go to Login.</Link>
          </Button>
        </CardContent>
      </CenteredCard>
    );
  }

  if (done) {
    return (
      <CenteredCard>
        <CardHeader>
          <CardTitle>Account Confirmed</CardTitle>
          <CardDescription>
            Your password has been set. You can now log in.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button
            className="w-full"
            onClick={() => navigate("/login", { replace: true })}
          >
            Go to Login.
          </Button>
        </CardContent>
      </CenteredCard>
    );
  }

  const validate = (): string | null => {
    if (password.length < MIN_PASSWORD)
      return `Password must be at least ${MIN_PASSWORD} characters long.`;
    if (password !== confirm) return "Passwords do not match.";
    return null;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const v = validate();
    if (v) {
      setError(v);
      return;
    }

    setError(null);
    setSubmitting(true);
    try {
      await confirmAccount(token, password);
      setDone(true);
    } catch (err) {
      if (err instanceof ApiError && err.status === 400) {
        setError(
          "Invalid or expired confirmation link. Please request a new confirmation email.",
        );
      } else {
        setError("Could not confirm the account. Please try again.");
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <CenteredCard>
      <CardHeader>
        <CardTitle className="text-2xl">Confirm Account</CardTitle>
        <CardDescription>
          Define your password to activate the account.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4" noValidate>
          <FormField
            id="password"
            label="Password"
            type="password"
            autoComplete="new-password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            disabled={submitting}
          />
          <FormField
            id="confirm"
            label="Confirm Password"
            type="password"
            autoComplete="new-password"
            required
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            disabled={submitting}
          />

          {error && (
            <p
              role="alert"
              className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-3 py-2"
            >
              {error}
            </p>
          )}

          <Button type="submit" className="w-full" disabled={submitting}>
            {submitting ? "Confirming…" : "Confirm Account"}
          </Button>
        </form>
      </CardContent>
    </CenteredCard>
  );
}

function CenteredCard({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-muted/30 px-4">
      <Card className="w-full max-w-sm">{children}</Card>
    </div>
  );
}
