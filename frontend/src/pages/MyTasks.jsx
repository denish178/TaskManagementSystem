import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import PageHeader from "../components/common/PageHeader";
import ErrorState from "../components/common/ErrorState";
import Skeleton from "../components/common/Skeleton";
import StatusBadge from "../components/common/StatusBadge";
import PriorityBadge from "../components/common/PriorityBadge";
import { inputClassName } from "../components/common/Field";
import taskService from "../services/taskService";
import { useAuth } from "../context/AuthContext";
import { getField } from "../utils/http";
import { formatDate } from "../utils/format";
import { TASK_STATUSES } from "../utils/constants";
import useDebounce from "../hooks/useDebounce";

export default function MyTasks() {
  const { user } = useAuth();
  const [tasks, setTasks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("");
  const debounced = useDebounce(query);

  async function load() {
    if (!user?.id) return;
    setLoading(true);
    setError("");
    try {
      const filters = { assigneeId: user.id };
      if (status) filters.status = status;
      setTasks(await taskService.list(filters));
    } catch (err) {
      setError(err.normalized?.message || "Unable to load tasks.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, [user?.id, status]);

  const filtered = useMemo(() => {
    const needle = debounced.trim().toLowerCase();
    if (!needle) return tasks;
    return tasks.filter((task) =>
      String(getField(task, "title") || "").toLowerCase().includes(needle),
    );
  }, [tasks, debounced]);

  if (loading) return <Skeleton className="h-64" />;
  if (error) return <ErrorState message={error} onRetry={load} />;

  return (
    <div>
      <PageHeader title="My Tasks" subtitle="All tasks assigned to you." />
      <div className="mb-4 flex flex-wrap gap-3">
        <input
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Search tasks"
          className={`${inputClassName} max-w-xs`}
        />
        <select
          className={inputClassName}
          value={status}
          onChange={(event) => setStatus(event.target.value)}
          aria-label="Filter by status"
        >
          <option value="">All statuses</option>
          {TASK_STATUSES.map((item) => (
            <option key={item.value} value={item.value}>{item.label}</option>
          ))}
        </select>
      </div>
      <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <table className="min-w-full text-left text-sm">
          <thead className="border-b border-slate-200 text-slate-500 dark:border-slate-800">
            <tr>
              <th className="px-4 py-3 font-medium">Task</th>
              <th className="px-4 py-3 font-medium">Status</th>
              <th className="px-4 py-3 font-medium">Priority</th>
              <th className="px-4 py-3 font-medium">Due</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((task) => (
              <tr key={getField(task, "id")} className="border-b border-slate-100 dark:border-slate-800">
                <td className="px-4 py-3">
                  <Link to={`/tasks/${getField(task, "id")}`} className="font-medium">
                    {getField(task, "title")}
                  </Link>
                </td>
                <td className="px-4 py-3">
                  <StatusBadge status={getField(task, "status")} />
                </td>
                <td className="px-4 py-3">
                  <PriorityBadge priority={getField(task, "priority")} />
                </td>
                <td className="px-4 py-3 text-slate-500">
                  {formatDate(getField(task, "dueDate"))}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
