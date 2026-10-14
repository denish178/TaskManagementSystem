export default function Field({ id, label, error, children, hint }) {
  return (
    <label className="block" htmlFor={id}>
      <span className="field-label">{label}</span>
      {children}
      {hint ? (
        <span className="mt-1 block text-xs text-[var(--app-text-muted)]">{hint}</span>
      ) : null}
      {error ? (
        <span className="mt-1 block text-xs text-[var(--app-danger)]" role="alert">
          {error}
        </span>
      ) : null}
    </label>
  );
}

export const inputClassName = "input";
