import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import PageHeader from "../components/common/PageHeader";
import ErrorState from "../components/common/ErrorState";
import { CardSkeletonGrid } from "../components/common/Skeleton";
import StatusBadge from "../components/common/StatusBadge";
import taskService from "../services/taskService";
import { useAuth } from "../context/AuthContext";
import { getField } from "../utils/http";
import { formatDate, isDueToday, isOverdue } from "../utils/format";

export default function MyWorkspace() {
  const { user } = useAuth();
  const [tasks, setTasks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    if (!user?.id) return;
    setLoading(true);
    setError("");
    try {
      const list = await taskService.list({ assigneeId: user.id });
      setTasks(list);
    } catch (err) {
      setError(err.normalized?.message || "Unable to load workspace.");
    } finally {
      setLoading(false);
    }
  }, [user]);

  useEffect(() => {
    load();
  }, [load]);

  const buckets = useMemo(() => {
    const overdue = [];
    const today = [];
    const inProgress = [];
    const done = [];
    tasks.forEach((task) => {
      const status = getField(task, "status");
      const due = getField(task, "dueDate");
      if (status === "Done") done.push(task);
      else if (isOverdue(due, status)) overdue.push(task);
      else if (isDueToday(due, status)) today.push(task);
      else if (status === "InProgress" || status === "Testing") inProgress.push(task);
    });
    return { overdue, today, inProgress, done };
  }, [tasks]);

  if (loading) {
    return (
      <div>
        <PageHeader title="My Workspace" subtitle="Work assigned to you." />
        <CardSkeletonGrid count={3} />
      </div>
    );
  }

  if (error) return <ErrorState message={error} onRetry={load} />;

  return (
    <div>
      <PageHeader
        title="My Workspace"
        subtitle="Tasks assigned to your account via the Task Service."
      />
      <div className="grid gap-6 lg:grid-cols-2">
        <TaskGroup title="Overdue" tasks={buckets.overdue} empty="Nothing overdue." />
        <TaskGroup title="Due today" tasks={buckets.today} empty="No tasks due today." />
        <TaskGroup title="In progress" tasks={buckets.inProgress} empty="No active work." />
        <TaskGroup title="Recently done" tasks={buckets.done.slice(0, 8)} empty="No completed tasks." />
      </div>
    </div>
  );
}

function TaskGroup({ title, tasks, empty }) {
  return (
    <section className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900">
      <h2 className="mb-3 font-semibold">{title}</h2>
      {tasks.length === 0 ? (
        <p className="text-sm text-slate-500">{empty}</p>
      ) : (
        <ul className="space-y-3">
          {tasks.map((task) => (
            <li key={getField(task, "id")} className="flex items-center justify-between gap-3">
              <Link to={`/tasks/${getField(task, "id")}`} className="text-sm font-medium">
                {getField(task, "title")}
              </Link>
              <div className="flex items-center gap-2 text-xs text-slate-500">
                <StatusBadge status={getField(task, "status")} />
                <span>{formatDate(getField(task, "dueDate"))}</span>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
