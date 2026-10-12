import { statusLabel } from "../../utils/format";

const styles = {
  Todo: "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200",
  InProgress: "bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-200",
  Testing: "bg-amber-50 text-amber-800 dark:bg-amber-950 dark:text-amber-200",
  Done: "bg-emerald-50 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200",
};

export default function StatusBadge({ status }) {
  return (
    <span
      className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-semibold ${styles[status] || styles.Todo}`}
    >
      {statusLabel(status)}
    </span>
  );
}
