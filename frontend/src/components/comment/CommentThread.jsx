import { useState } from "react";
import { toast } from "react-toastify";
import Button from "../common/Button";
import EmptyState from "../common/EmptyState";
import { inputClassName } from "../common/Field";
import commentService from "../../services/commentService";
import { formatDateTime } from "../../utils/format";
import { getField } from "../../utils/http";
import { MessageSquare } from "lucide-react";

export default function CommentThread({
  taskId,
  comments,
  usersById,
  currentUser,
  onChange,
}) {
  const [content, setContent] = useState("");
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [draft, setDraft] = useState("");

  async function addComment(event) {
    event.preventDefault();
    if (!content.trim()) return;
    setSaving(true);
    try {
      await commentService.create({ taskId, content: content.trim() });
      setContent("");
      toast.success("Comment added");
      onChange();
    } catch (error) {
      toast.error(error.normalized?.message || "Unable to add comment.");
    } finally {
      setSaving(false);
    }
  }

  async function saveEdit(commentId) {
    if (!draft.trim()) return;
    try {
      await commentService.update(commentId, { content: draft.trim() });
      setEditingId(null);
      toast.success("Comment updated");
      onChange();
    } catch (error) {
      toast.error(error.normalized?.message || "Unable to update comment.");
    }
  }

  async function remove(commentId) {
    try {
      await commentService.remove(commentId);
      toast.success("Comment deleted");
      onChange();
    } catch (error) {
      toast.error(error.normalized?.message || "Unable to delete comment.");
    }
  }

  return (
    <section>
      <h2 className="mb-3 text-base font-semibold">Comments</h2>
      {comments.length === 0 ? (
        <EmptyState
          icon={MessageSquare}
          title="No comments yet"
          description="Start the discussion for this task."
        />
      ) : (
        <ul className="space-y-3">
          {comments.map((comment) => {
            const id = getField(comment, "id");
            const userId = getField(comment, "userId");
            const canManage = currentUser?.id === userId;
            const author = usersById[userId];
            return (
              <li
                key={id}
                className="rounded-xl border border-slate-200 p-3 dark:border-slate-800"
              >
                <div className="mb-2 flex items-center justify-between gap-2 text-xs text-slate-500">
                  <span>{author?.name || "User"}</span>
                  <span>{formatDateTime(getField(comment, "createdAt"))}</span>
                </div>
                {editingId === id ? (
                  <div className="space-y-2">
                    <textarea
                      className={inputClassName}
                      value={draft}
                      onChange={(event) => setDraft(event.target.value)}
                    />
                    <div className="flex gap-2">
                      <Button onClick={() => saveEdit(id)}>Save</Button>
                      <Button variant="secondary" onClick={() => setEditingId(null)}>
                        Cancel
                      </Button>
                    </div>
                  </div>
                ) : (
                  <p className="whitespace-pre-wrap text-sm">{getField(comment, "content")}</p>
                )}
                {canManage && editingId !== id ? (
                  <div className="mt-2 flex gap-2">
                    <Button
                      variant="ghost"
                      onClick={() => {
                        setEditingId(id);
                        setDraft(getField(comment, "content") || "");
                      }}
                    >
                      Edit
                    </Button>
                    <Button variant="ghost" onClick={() => remove(id)}>
                      Delete
                    </Button>
                  </div>
                ) : null}
              </li>
            );
          })}
        </ul>
      )}
      <form className="mt-4 space-y-2" onSubmit={addComment}>
        <label htmlFor="comment" className="sr-only">
          Add a comment
        </label>
        <textarea
          id="comment"
          className={inputClassName}
          rows={3}
          value={content}
          onChange={(event) => setContent(event.target.value)}
          placeholder="Write a comment"
        />
        <Button type="submit" disabled={saving || !content.trim()}>
          {saving ? "Posting..." : "Post comment"}
        </Button>
      </form>
    </section>
  );
}
