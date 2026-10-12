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
    <div className="grid min-h-screen place-items-center bg-slate-50 px-4 dark:bg-slate-950">
      <form
        className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 shadow-sm dark:border-slate-800 dark:bg-slate-900"
        onSubmit={handleSubmit(onSubmit)}
        noValidate
      >
        <div className="mb-6 flex items-center gap-3">
          <span className="grid h-10 w-10 place-items-center rounded-xl bg-indigo-600 font-bold text-white">
            T
          </span>
          <span className="text-lg font-semibold">TaskFlow</span>
        </div>
        <h1 className="text-2xl font-semibold">Welcome back</h1>
        <p className="mt-1 mb-6 text-sm text-slate-500">
          Sign in through the API Gateway with your existing account.
        </p>
        {!getApiBaseUrl() ? (
          <p className="mb-4 rounded-lg bg-amber-50 p-3 text-sm text-amber-800">
            VITE_API_BASE_URL is not configured.
          </p>
        ) : null}
        {error ? (
          <p className="mb-4 rounded-lg bg-rose-50 p-3 text-sm text-rose-700" role="alert">
            {error}
          </p>
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
        <p className="mt-4 text-center text-sm text-slate-500">
          Need an account?{" "}
          <Link className="font-medium text-indigo-600" to="/register">
            Register
          </Link>
        </p>
      </form>
    </div>
  );
}
