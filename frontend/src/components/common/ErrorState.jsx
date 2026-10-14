import Button from "./Button";

export default function ErrorState({ message, onRetry }) {
  return (
    <div role="alert" className="alert-error card-pad">
      <p className="text-sm">{message || "Unable to load this data."}</p>
      {onRetry ? (
        <Button className="mt-3" variant="secondary" onClick={onRetry}>
          Retry
        </Button>
      ) : null}
    </div>
  );
}
