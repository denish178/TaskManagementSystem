import { useCallback, useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { toast } from "react-toastify";
import PageHeader from "../components/common/PageHeader";
import ProjectSubnav from "../components/layout/ProjectSubnav";
import ErrorState from "../components/common/ErrorState";
import Skeleton from "../components/common/Skeleton";
import Button from "../components/common/Button";
import Modal from "../components/common/Modal";
import KanbanBoard from "../components/task/KanbanBoard";
import TaskForm from "../components/task/TaskForm";
import { inputClassName } from "../components/common/Field";
import projectService from "../services/projectService";
import taskService from "../services/taskService";
import sprintService from "../services/sprintService";
import authService from "../services/authService";
import { getField } from "../utils/http";
import { TASK_STATUSES } from "../utils/constants";
import { toIsoDate } from "../utils/format";
import useDebounce from "../hooks/useDebounce";
import { connectTaskHub } from "../services/realtimeService";

export default function ProjectTasks() {
  const { projectId } = useParams();
  const [project, setProject] = useState(null);
  const [tasks, setTasks] = useState([]);
  const [sprints, setSprints] = useState([]);
  const [usersById, setUsersById] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [statusBusy, setStatusBusy] = useState(false);
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const debounced = useDebounce(query);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const [projectData, taskData, sprintData, users] = await Promise.all([
        projectService.getById(projectId),
        taskService.list({ projectId }),
        sprintService.listByProject(projectId),
        authService.getUsers(),
      ]);
      setProject(projectData);
      setTasks(taskData);
      setSprints(sprintData);
      setUsersById(
        Object.fromEntries(users.map((user) => [user.id || user.Id, user])),
      );
    } catch (err) {
      setError(err.normalized?.message || "Unable to load board.");
    } finally {
      setLoading(false);
    }
  }, [projectId]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (loading || !projectId) return undefined;

    const hub = connectTaskHub(projectId, {
      onStatusChanged: (payload) => {
        setTasks((current) =>
          current.map((task) =>
            String(getField(task, "id")) === String(payload.id)
              ? { ...task, status: payload.status, updatedAt: payload.updatedAt }
              : task,
          ),
        );
      },
      onAssigned: (payload) => {
        setTasks((current) =>
          current.map((task) =>
            String(getField(task, "id")) === String(payload.id)
              ? { ...task, assigneeId: payload.assigneeId, updatedAt: payload.updatedAt }
              : task,
          ),
        );
      },
    });

    hub.start().catch(() => {});

    return () => {
      hub.stop().catch(() => {});
    };
  }, [projectId, loading]);

  const filtered = useMemo(() => {
    const needle = debounced.trim().toLowerCase();
    return tasks.filter((task) => {
      if (statusFilter && getField(task, "status") !== statusFilter) return false;
      if (!needle) return true;
      return String(getField(task, "title") || "")
        .toLowerCase()
        .includes(needle);
    });
  }, [tasks, debounced, statusFilter]);

  async function createTask(values) {
    setSaving(true);
    try {
      await taskService.create({
        projectId,
        title: values.title.trim(),
        description: values.description || null,
        priority: values.priority || "Medium",
        sprintId: values.sprintId || null,
        dueDate: toIsoDate(values.dueDate),
      });
      toast.success("Task created");
      setCreateOpen(false);
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to create task.");
    } finally {
      setSaving(false);
    }
  }

  async function handleStatusChange(taskId, nextStatus, previousStatus) {
    setStatusBusy(true);
    setTasks((current) =>
      current.map((task) =>
        String(getField(task, "id")) === String(taskId)
          ? { ...task, status: nextStatus }
          : task,
      ),
    );
    try {
      await taskService.updateStatus(taskId, nextStatus);
      toast.success("Status updated");
    } catch (err) {
      setTasks((current) =>
        current.map((task) =>
          String(getField(task, "id")) === String(taskId)
            ? { ...task, status: previousStatus }
            : task,
        ),
      );
      toast.error(err.normalized?.message || "Status update failed.");
    } finally {
      setStatusBusy(false);
    }
  }

  if (loading) return <Skeleton className="h-64" />;
  if (error) return <ErrorState message={error} onRetry={load} />;

  return (
    <div>
      <PageHeader
        crumbs="Projects"
        title={getField(project, "name")}
        subtitle="Kanban board"
        actions={<Button onClick={() => setCreateOpen(true)}>New task</Button>}
      />
      <ProjectSubnav projectId={projectId} />
      <div className="mb-4 flex flex-wrap gap-3">
        <input
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Search tasks"
          className={`${inputClassName} max-w-xs`}
          aria-label="Search tasks"
        />
        <select
          id="statusFilter"
          className={inputClassName}
          value={statusFilter}
          onChange={(event) => setStatusFilter(event.target.value)}
          aria-label="Filter by status"
        >
          <option value="">All statuses</option>
          {TASK_STATUSES.map((status) => (
            <option key={status.value} value={status.value}>
              {status.label}
            </option>
          ))}
        </select>
      </div>
      <KanbanBoard
        tasks={filtered}
        usersById={usersById}
        onStatusChange={handleStatusChange}
        disabled={statusBusy}
      />
      <Modal open={createOpen} title="Create task" onClose={() => setCreateOpen(false)} wide>
        <TaskForm
          sprints={sprints}
          submitting={saving}
          onCancel={() => setCreateOpen(false)}
          onSubmit={createTask}
        />
      </Modal>
    </div>
  );
}
