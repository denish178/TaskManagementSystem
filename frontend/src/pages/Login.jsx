import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useAuth } from "../context/AuthContext";
import Field, { inputClassName } from "../components/common/Field";
import Button from "../components/common/Button";
import { getApiBaseUrl } from "../services/api";

const schema = z.object({
  email: z.string().email("Enter a valid email"),
  password: z.string().min(1, "Password is required"),
});

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [error, setError] = useState("");
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm({ resolver: zodResolver(schema) });

  async function onSubmit(values) {
    setError("");
    try {
      await login(values.email.trim(), values.password);
      navigate(location.state?.from || "/dashboard", { replace: true });
    } catch (err) {
      setError(err.normalized?.message || "Unable to log in.");
    }
  }

  return (
    <div className="grid min-h-screen place-items-center px-4">
      <form className="auth-card" onSubmit={handleSubmit(onSubmit)} noValidate>
        <div className="mb-6 flex items-center gap-3">
          <span className="logo-mark h-10 w-10 text-base">T</span>
          <span className="text-base font-semibold tracking-tight">TaskFlow</span>
        </div>
        <h1 className="page-title">Welcome back</h1>
        <p className="page-lead mb-6">
          Sign in with your team account. Requests go through the API gateway on port 5204.
        </p>
        {!getApiBaseUrl() ? (
          <p className="alert-warn mb-4">VITE_API_BASE_URL is not configured.</p>
        ) : null}
        {error ? (
          <p className="alert-error mb-4" role="alert">{error}</p>
        ) : null}
        <div className="space-y-4">
          <Field id="email" label="Email address" error={errors.email?.message}>
            <input
              id="email"
              type="email"
              autoComplete="email"
              className={inputClassName}
              {...register("email")}
            />
          </Field>
          <Field id="password" label="Password" error={errors.password?.message}>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              className={inputClassName}
              {...register("password")}
            />
          </Field>
        </div>
        <Button type="submit" className="mt-6 w-full" disabled={isSubmitting}>
          {isSubmitting ? "Signing in..." : "Sign in"}
        </Button>
        <p className="mt-4 text-center text-sm text-[var(--app-text-muted)]">
          Need an account?{" "}
          <Link className="text-link" to="/register">
            Register
          </Link>
        </p>
      </form>
    </div>
  );
}
