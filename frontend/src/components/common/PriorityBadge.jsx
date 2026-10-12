const styles = {
  Low: "bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300",
  Medium: "bg-sky-50 text-sky-700 dark:bg-sky-950 dark:text-sky-200",
  High: "bg-orange-50 text-orange-700 dark:bg-orange-950 dark:text-orange-200",
  Critical: "bg-rose-50 text-rose-700 dark:bg-rose-950 dark:text-rose-200",
};

export default function PriorityBadge({ priority }) {
  const value = priority || "Medium";
  return (
    <span
      className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-semibold ${styles[value] || styles.Medium}`}
    >
      {value}
    </span>
  );
}
