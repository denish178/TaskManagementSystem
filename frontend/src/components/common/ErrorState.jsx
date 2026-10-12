import Button from "./Button";

export default function ErrorState({ message, onRetry }) {
  return (
    <div
      role="alert"
      className="rounded-2xl border border-rose-200 bg-rose-50 p-5 text-rose-800 dark:border-rose-900 dark:bg-rose-950/40 dark:text-rose-200"
    >
      <p className="text-sm">{message || "Unable to load this data."}</p>
      {onRetry ? (
        <Button className="mt-3" variant="secondary" onClick={onRetry}>
          Retry
        </Button>
      ) : null}
    </div>
  );
}
