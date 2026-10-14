import { initials } from "../../utils/format";

export default function Avatar({ name, size = "md" }) {
  const sizes = {
    sm: "h-7 w-7 text-[10px]",
    md: "h-9 w-9 text-xs",
    lg: "h-11 w-11 text-sm",
  };

  return (
    <span
      className={`inline-grid shrink-0 place-items-center rounded-full bg-[var(--app-accent-soft)] font-semibold text-[var(--app-accent-text)] ${sizes[size]}`}
      aria-hidden="true"
    >
      {initials(name)}
    </span>
  );
}
