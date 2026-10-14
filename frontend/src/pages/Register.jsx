import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import authService from "../services/authService";
import Field, { inputClassName } from "../components/common/Field";
import Button from "../components/common/Button";

const schema = z.object({
  name: z.string().trim().min(1, "Name is required").max(100),
  email: z.string().email("Enter a valid email"),
  password: z.string().min(8, "Password must be at least 8 characters"),
});

export default function Register() {
  const navigate = useNavigate();
  const [error, setError] = useState("");
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm({ resolver: zodResolver(schema) });

  async function onSubmit(values) {
    setError("");
    try {
      await authService.register(values);
      navigate("/login", { replace: true, state: { registered: true } });
    } catch (err) {
      setError(err.normalized?.message || "Unable to register.");
    }
  }

  return (
    <div className="grid min-h-screen place-items-center px-4">
      <form className="auth-card" onSubmit={handleSubmit(onSubmit)} noValidate>
        <h1 className="page-title">Create an account</h1>
        <p className="page-lead mb-6">New accounts are created with the User role.</p>
        {error ? (
          <p className="alert-error mb-4" role="alert">{error}</p>
        ) : null}
        <div className="space-y-4">
          <Field id="name" label="Name" error={errors.name?.message}>
            <input id="name" className={inputClassName} {...register("name")} />
          </Field>
          <Field id="email" label="Email" error={errors.email?.message}>
            <input id="email" type="email" className={inputClassName} {...register("email")} />
          </Field>
          <Field id="password" label="Password" error={errors.password?.message}>
            <input
              id="password"
              type="password"
              className={inputClassName}
              {...register("password")}
            />
          </Field>
        </div>
        <Button type="submit" className="mt-6 w-full" disabled={isSubmitting}>
          {isSubmitting ? "Creating..." : "Register"}
        </Button>
        <p className="mt-4 text-center text-sm text-[var(--app-text-muted)]">
          Already registered?{" "}
          <Link className="text-link" to="/login">
            Sign in
          </Link>
        </p>
      </form>
    </div>
  );
}
