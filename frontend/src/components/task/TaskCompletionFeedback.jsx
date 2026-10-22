import { useEffect, useState } from "react";
import { toast } from "react-toastify";
import Button from "../common/Button";
import { inputClassName } from "../common/Field";
import commentService from "../../services/commentService";
import { getField } from "../../utils/http";
import { formatDateTime } from "../../utils/format";

export default function TaskCompletionFeedback({
  taskId,
  task,
  feedbackRecord,
  isAdmin,
  usersById = {},
  onSaved,
}) {
  const status = getField(task, "status");
  const canSubmit = Boolean(getField(task, "canSubmitCompletionFeedback"));
  const feedback =
    getField(feedbackRecord, "content") || getField(feedbackRecord, "Content");
  const submittedAt =
    getField(feedbackRecord, "submittedAt")
    || getField(feedbackRecord, "SubmittedAt");
  const completedBy =
    getField(task, "completedByUserId")
    || getField(feedbackRecord, "submittedByUserId")
    || getField(feedbackRecord, "SubmittedByUserId");
  const completerName =
    usersById[completedBy]?.name ||
    usersById[completedBy]?.fullName ||
    usersById[completedBy]?.email;
  const [draft, setDraft] = useState(feedback || "");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setDraft(feedback || "");
  }, [feedback, taskId]);

  const isDone = status === "Done";

  if (isAdmin) {
    return (
      <section className="card card-pad">
        <h2 className="section-title mb-2">Completion feedback</h2>
        {!isDone ? (
          <p className="text-sm text-[var(--app-text-muted)]">
            Feedback appears here after the task is marked Done.
          </p>
        ) : feedback ? (
          <>
            <p className="whitespace-pre-wrap text-sm leading-6">{feedback}</p>
            <p className="mt-2 text-xs text-[var(--app-text-muted)]">
              Submitted {formatDateTime(submittedAt)}
              {completerName
                ? ` · ${completerName}`
                : completedBy
                  ? ` · completer ${completedBy.slice(0, 8)}…`
                  : ""}
            </p>
          </>
        ) : (
          <p className="text-sm text-[var(--app-text-muted)]">
            No feedback from the assignee yet.
          </p>
        )}
      </section>
    );
  }

  return (
    <section className="card card-pad">
      <h2 className="section-title mb-2">Completion feedback</h2>
      <p className="mb-3 text-sm text-[var(--app-text-muted)]">
        Share a short summary for your admin once the task is Done. Only administrators
        can read this.
      </p>
      {!isDone ? (
        <p className="rounded-md border border-[var(--app-border)] bg-[var(--app-bg)] px-3 py-2 text-sm text-[var(--app-text-muted)]">
          Feedback is disabled until the task status is <strong>Done</strong>.
        </p>
      ) : (
        <form
          onSubmit={async (event) => {
            event.preventDefault();
            if (!draft.trim() || !canSubmit) return;
            setSaving(true);
            try {
              await commentService.submitCompletionFeedback(taskId, draft.trim());
              toast.success("Feedback saved");
              onSaved();
            } catch (err) {
              toast.error(err.normalized?.message || "Unable to save feedback.");
            } finally {
              setSaving(false);
            }
          }}
          className="space-y-3"
        >
          <textarea
            className={inputClassName}
            rows={4}
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            placeholder="What did you deliver? Any blockers or notes for the admin?"
            disabled={!canSubmit || saving}
          />
          <Button type="submit" disabled={!canSubmit || saving || !draft.trim()}>
            {saving ? "Saving…" : feedback ? "Update feedback" : "Submit feedback"}
          </Button>
          {!canSubmit && isDone ? (
            <p className="text-xs text-[var(--app-text-muted)]">
              Only the team member who marked this task Done can submit feedback.
            </p>
          ) : null}
        </form>
      )}
    </section>
  );
}
