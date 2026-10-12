import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { toast } from "react-toastify";
import PageHeader from "../components/common/PageHeader";
import ErrorState from "../components/common/ErrorState";
import Skeleton from "../components/common/Skeleton";
import Button from "../components/common/Button";
import Modal from "../components/common/Modal";
import ConfirmDialog from "../components/common/ConfirmDialog";
import StatusBadge from "../components/common/StatusBadge";
import PriorityBadge from "../components/common/PriorityBadge";
import TaskForm from "../components/task/TaskForm";
import CommentThread from "../components/comment/CommentThread";
import FilePanel from "../components/file/FilePanel";
import Field, { inputClassName } from "../components/common/Field";
import taskService from "../services/taskService";
import sprintService from "../services/sprintService";
import commentService from "../services/commentService";
import fileService from "../services/fileService";
import authService from "../services/authService";
import { useAuth } from "../context/AuthContext";
import { getField } from "../utils/http";
import { formatDate, formatDateTime, shortId, toIsoDate } from "../utils/format";
import { TASK_STATUSES } from "../utils/constants";
import { connectCommentHub } from "../services/realtimeService";

export default function TaskDetails() {
  const { taskId } = useParams();
  const navigate = useNavigate();
  const { user, isAdmin } = useAuth();
  const [task, setTask] = useState(null);
  const [sprints, setSprints] = useState([]);
  const [comments, setComments] = useState([]);
  const [files, setFiles] = useState([]);
  const [users, setUsers] = useState([]);
  const [usersById, setUsersById] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [subtaskTitle, setSubtaskTitle] = useState("");
  const [assigneeId, setAssigneeId] = useState("");
  const [auditTrail, setAuditTrail] = useState([]);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const taskData = await taskService.getById(taskId);
      setTask(taskData);
      setAssigneeId(getField(taskData, "assigneeId") || "");
      const projectId = getField(taskData, "projectId");
      const [commentList, fileList, userList, sprintList, auditList] =
        await Promise.all([
        commentService.listByTask(taskId),
        fileService.listByTask(taskId),
        authService.getUsers(),
        projectId ? sprintService.listByProject(projectId) : Promise.resolve([]),
        taskService.getAudit(taskId),
      ]);
      setComments(commentList);
      setAuditTrail(auditList);
      setFiles(fileList);
      setUsers(userList);
      setUsersById(
        Object.fromEntries(userList.map((u) => [u.id || u.Id, u])),
      );
      setSprints(sprintList);
    } catch (err) {
      setError(err.normalized?.message || "Unable to load task.");
    } finally {
      setLoading(false);
    }
  }, [taskId]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (loading || !taskId) return undefined;

    const hub = connectCommentHub(taskId, {
      onCommentAdded: (comment) => {
        setComments((current) => {
          const id = comment.id || comment.Id;
          if (current.some((c) => (c.id || c.Id) === id)) return current;
          return [...current, comment];
        });
      },
    });

    hub.start().catch(() => {});

    return () => {
      hub.stop().catch(() => {});
    };
  }, [taskId, loading]);

  async function saveTask(values) {
    setSaving(true);
    try {
      await taskService.update(taskId, {
        title: values.title.trim(),
        description: values.description || null,
        priority: values.priority || "Medium",
        sprintId: values.sprintId || null,
        dueDate: toIsoDate(values.dueDate),
      });
      toast.success("Task updated");
      setEditOpen(false);
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to update task.");
    } finally {
      setSaving(false);
    }
  }

  async function changeStatus(status) {
    try {
      await taskService.updateStatus(taskId, status);
      toast.success("Status updated");
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Status update failed.");
    }
  }

  async function assignTask() {
    if (!isAdmin || !assigneeId) return;
    setSaving(true);
    try {
      await taskService.assign(taskId, assigneeId);
      toast.success("Task assigned");
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Assignment failed.");
    } finally {
      setSaving(false);
    }
  }

  async function removeTask() {
    setSaving(true);
    try {
      await taskService.remove(taskId);
      toast.success("Task deleted");
      navigate(`/projects/${getField(task, "projectId")}/tasks`, { replace: true });
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to delete task.");
    } finally {
      setSaving(false);
    }
  }

  async function addSubtask(event) {
    event.preventDefault();
    if (!subtaskTitle.trim()) return;
    try {
      await taskService.createSubtask(taskId, { title: subtaskTitle.trim() });
      setSubtaskTitle("");
      toast.success("Subtask added");
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to add subtask.");
    }
  }

  async function toggleSubtask(subtask) {
    try {
      await taskService.updateSubtask(taskId, getField(subtask, "id"), {
        title: getField(subtask, "title"),
        isCompleted: !getField(subtask, "isCompleted"),
      });
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to update subtask.");
    }
  }

  async function deleteSubtask(subtask) {
    try {
      await taskService.removeSubtask(taskId, getField(subtask, "id"));
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to delete subtask.");
    }
  }

  if (loading) return <Skeleton className="h-64" />;
  if (error) return <ErrorState message={error} onRetry={load} />;
  if (!task) return null;

  const subtasks = getField(task, "subTasks") || getField(task, "SubTasks") || [];
  const assignee = usersById[getField(task, "assigneeId")];
  const projectId = getField(task, "projectId");

  return (
    <div>
      <PageHeader
        crumbs={
          <>
            <Link to="/projects">Projects</Link>
            {projectId ? (
              <>
                {" / "}
                <Link to={`/projects/${projectId}`}>Project</Link>
              </>
            ) : null}
          </>
        }
        title={getField(task, "title")}
        subtitle={`Task ${shortId(taskId)}`}
        actions={
          <>
            <Button variant="secondary" onClick={() => setEditOpen(true)}>
              Edit
            </Button>
            <Button variant="ghost" onClick={() => setDeleteOpen(true)}>
              Delete
            </Button>
          </>
        }
      />

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <section className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900">
            <p className="whitespace-pre-wrap text-sm leading-6 text-slate-700 dark:text-slate-300">
              {getField(task, "description") || "No description"}
            </p>
          </section>

          <section className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900">
            <h2 className="mb-3 font-semibold">Subtasks</h2>
            <ul className="space-y-2">
              {subtasks.map((subtask) => (
                <li key={getField(subtask, "id")} className="flex items-center gap-3">
                  <input
                    type="checkbox"
                    checked={Boolean(getField(subtask, "isCompleted"))}
                    onChange={() => toggleSubtask(subtask)}
                    aria-label={`Mark ${getField(subtask, "title")} complete`}
                  />
                  <span className="flex-1 text-sm">{getField(subtask, "title")}</span>
                  <Button variant="ghost" onClick={() => deleteSubtask(subtask)}>
                    Remove
                  </Button>
                </li>
              ))}
            </ul>
            <form className="mt-4 flex gap-2" onSubmit={addSubtask}>
              <input
                className={inputClassName}
                value={subtaskTitle}
                onChange={(event) => setSubtaskTitle(event.target.value)}
                placeholder="New subtask"
              />
              <Button type="submit">Add</Button>
            </form>
          </section>

          <div className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900">
            <CommentThread
              taskId={taskId}
              comments={comments}
              usersById={usersById}
              currentUser={user}
              onChange={load}
            />
          </div>

          <div className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900">
            <FilePanel
              taskId={taskId}
              files={files}
              currentUser={user}
              usersById={usersById}
              onChange={load}
            />
          </div>
        </div>

        <aside className="space-y-4">
          <MetaCard title="Status">
            <StatusBadge status={getField(task, "status")} />
            <Field id="status" label="Change status">
              <select
                id="status"
                className={inputClassName}
                value={getField(task, "status")}
                onChange={(event) => changeStatus(event.target.value)}
              >
                {TASK_STATUSES.map((s) => (
                  <option key={s.value} value={s.value}>{s.label}</option>
                ))}
              </select>
            </Field>
          </MetaCard>
          <MetaCard title="Priority">
            <PriorityBadge priority={getField(task, "priority")} />
          </MetaCard>
          <MetaCard title="Due date">
            <p className="text-sm">{formatDate(getField(task, "dueDate"))}</p>
          </MetaCard>
          <MetaCard title="Assignee">
            <p className="text-sm">{assignee?.name || "Unassigned"}</p>
            {isAdmin ? (
              <div className="mt-2 space-y-2">
                <select
                  className={inputClassName}
                  value={assigneeId}
                  onChange={(event) => setAssigneeId(event.target.value)}
                  aria-label="Assignee"
                >
                  <option value="">Select user</option>
                  {users.map((u) => (
                    <option key={u.id || u.Id} value={u.id || u.Id}>
                      {u.name}
                    </option>
                  ))}
                </select>
                <Button onClick={assignTask} disabled={!assigneeId || saving}>
                  Assign
                </Button>
              </div>
            ) : (
              <p className="mt-2 text-xs text-slate-500">
                Only Admin users can assign or reassign tasks.
              </p>
            )}
          </MetaCard>
          <MetaCard title="Updated">
            <p className="text-sm">{formatDateTime(getField(task, "updatedAt"))}</p>
          </MetaCard>
          <MetaCard title="Activity">
            {auditTrail.length === 0 ? (
              <p className="text-sm text-slate-500">No status or assignment changes yet.</p>
            ) : (
              <ul className="max-h-48 space-y-2 overflow-y-auto text-sm">
                {auditTrail.map((entry) => {
                  const actor =
                    usersById[entry.actorUserId || entry.ActorUserId]?.name ||
                    shortId(entry.actorUserId || entry.ActorUserId);
                  const action = entry.action || entry.Action;
                  const oldVal = entry.oldValue ?? entry.OldValue;
                  const newVal = entry.newValue ?? entry.NewValue;
                  const when = entry.createdAtUtc || entry.CreatedAtUtc;
                  return (
                    <li key={entry.id || entry.Id} className="text-slate-600 dark:text-slate-300">
                      <span className="font-medium">{actor}</span>
                      {" · "}
                      {action === "Assigned"
                        ? `assigned (${shortId(oldVal)} → ${shortId(newVal)})`
                        : `status ${oldVal} → ${newVal}`}
                      <div className="text-xs text-slate-400">
                        {formatDateTime(when)}
                      </div>
                    </li>
                  );
                })}
              </ul>
            )}
          </MetaCard>
        </aside>
      </div>

      <Modal open={editOpen} title="Edit task" onClose={() => setEditOpen(false)} wide>
        <TaskForm
          defaultValues={task}
          sprints={sprints}
          submitting={saving}
          onCancel={() => setEditOpen(false)}
          onSubmit={saveTask}
        />
      </Modal>

      <ConfirmDialog
        open={deleteOpen}
        title="Delete task"
        message="Delete this task and its subtasks?"
        confirmLabel="Delete"
        loading={saving}
        onClose={() => setDeleteOpen(false)}
        onConfirm={removeTask}
      />
    </div>
  );
}

function MetaCard({ title, children }) {
  return (
    <section className="rounded-2xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
      <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-400">
        {title}
      </h3>
      {children}
    </section>
  );
}
