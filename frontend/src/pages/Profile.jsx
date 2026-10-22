import { useEffect, useState } from "react";
import { toast } from "react-toastify";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import PageHeader from "../components/common/PageHeader";
import Button from "../components/common/Button";
import Field, { inputClassName } from "../components/common/Field";
import { useAuth } from "../context/AuthContext";
import { useTheme } from "../context/ThemeContext";
import authService from "../services/authService";
import { formatDateTime } from "../utils/format";

const profileSchema = z.object({
  name: z.string().trim().min(1).max(100),
  email: z.string().email(),
});

const passwordSchema = z
  .object({
    currentPassword: z.string().min(1),
    newPassword: z.string().min(8),
    confirmNewPassword: z.string().min(8),
  })
  .refine((data) => data.newPassword === data.confirmNewPassword, {
    message: "Passwords must match",
    path: ["confirmNewPassword"],
  });

export default function Profile() {
  const { user, refreshProfile } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const [profile, setProfile] = useState(null);

  const profileForm = useForm({
    resolver: zodResolver(profileSchema),
    defaultValues: { name: "", email: "" },
  });

  const passwordForm = useForm({
    resolver: zodResolver(passwordSchema),
    defaultValues: {
      currentPassword: "",
      newPassword: "",
      confirmNewPassword: "",
    },
  });

  useEffect(() => {
    authService
      .getProfile()
      .then((data) => {
        setProfile(data);
        profileForm.reset({ name: data.name, email: data.email });
      })
      .catch((err) => {
        toast.error(err.normalized?.message || "Unable to load profile.");
      });
  }, [profileForm]);

  async function saveProfile(values) {
    try {
      await authService.updateProfile(values);
      await refreshProfile();
      toast.success("Profile updated");
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to update profile.");
    }
  }

  async function savePassword(values) {
    try {
      await authService.changePassword(values);
      passwordForm.reset();
      toast.success("Password changed");
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to change password.");
    }
  }

  return (
    <div className="max-w-2xl space-y-8">
      <PageHeader title="Profile" />
      <section className="card card-pad">
        <p className="text-sm text-slate-500">
          Role: <strong>{user?.role}</strong>
          {profile?.createdAt ? (
            <> · Joined {formatDateTime(profile.createdAt)}</>
          ) : null}
        </p>
        <form
          className="mt-4 space-y-4"
          onSubmit={profileForm.handleSubmit(saveProfile)}
        >
          <Field
            id="name"
            label="Name"
            error={profileForm.formState.errors.name?.message}
          >
            <input id="name" className={inputClassName} {...profileForm.register("name")} />
          </Field>
          <Field
            id="email"
            label="Email"
            error={profileForm.formState.errors.email?.message}
          >
            <input id="email" className={inputClassName} {...profileForm.register("email")} />
          </Field>
          <Button type="submit" disabled={profileForm.formState.isSubmitting}>
            Save profile
          </Button>
        </form>
      </section>

      <section className="card card-pad">
        <h2 className="font-semibold">Preferences</h2>
        <p className="mt-1 text-sm text-slate-500">
          Theme is stored locally in your browser ({theme}).
        </p>
        <Button className="mt-3" variant="secondary" onClick={toggleTheme}>
          Switch to {theme === "dark" ? "light" : "dark"} mode
        </Button>
      </section>

      <section className="card card-pad">
        <h2 className="font-semibold">Change password</h2>
        <form
          className="mt-4 space-y-4"
          onSubmit={passwordForm.handleSubmit(savePassword)}
        >
          <Field
            id="currentPassword"
            label="Current password"
            error={passwordForm.formState.errors.currentPassword?.message}
          >
            <input
              id="currentPassword"
              type="password"
              className={inputClassName}
              {...passwordForm.register("currentPassword")}
            />
          </Field>
          <Field
            id="newPassword"
            label="New password"
            error={passwordForm.formState.errors.newPassword?.message}
          >
            <input
              id="newPassword"
              type="password"
              className={inputClassName}
              {...passwordForm.register("newPassword")}
            />
          </Field>
          <Field
            id="confirmNewPassword"
            label="Confirm new password"
            error={passwordForm.formState.errors.confirmNewPassword?.message}
          >
            <input
              id="confirmNewPassword"
              type="password"
              className={inputClassName}
              {...passwordForm.register("confirmNewPassword")}
            />
          </Field>
          <Button type="submit" disabled={passwordForm.formState.isSubmitting}>
            Update password
          </Button>
        </form>
      </section>
    </div>
  );
}
