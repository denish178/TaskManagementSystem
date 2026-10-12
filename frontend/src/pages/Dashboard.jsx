import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
} from "recharts";
import PageHeader from "../components/common/PageHeader";
import { CardSkeletonGrid } from "../components/common/Skeleton";
import ErrorState from "../components/common/ErrorState";
import EmptyState from "../components/common/EmptyState";
import StatusBadge from "../components/common/StatusBadge";
import projectService from "../services/projectService";
import taskService from "../services/taskService";
import sprintService from "../services/sprintService";
import dashboardService from "../services/dashboardService";
import { getField, isCancelError } from "../utils/http";
import { STATUS_COLORS, TASK_STATUSES } from "../utils/constants";
import { formatDate, isOverdue } from "../utils/format";
import { FolderKanban } from "lucide-react";

function countFromDashboard(counts = {}) {
  return {
    Todo: Number(counts.todo ?? counts.Todo ?? 0),
    InProgress: Number(counts.inProgress ?? counts.InProgress ?? 0),
    Testing: Number(counts.testing ?? counts.Testing ?? 0),
    Done: Number(counts.done ?? counts.Done ?? 0),
  };
}

function totalsFromTasks(taskList) {
  const totals = { Todo: 0, InProgress: 0, Testing: 0, Done: 0 };
  taskList.forEach((task) => {
    const status = getField(task, "status");
    if (status in totals) {
      totals[status] += 1;
    }
  });
  return totals;
}

export default function Dashboard() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [projects, setProjects] = useState([]);
  const [tasks, setTasks] = useState([]);
  const [statusTotals, setStatusTotals] = useState(null);
  const [overdueCount, setOverdueCount] = useState(null);
  const [sprintProgress, setSprintProgress] = useState([]);
  const [partial, setPartial] = useState("");

  async function load() {
    const controller = new AbortController();
    setLoading(true);
    setError("");
    setPartial("");
    try {
      const [projectList, taskList] = await Promise.all([
        projectService.list(controller.signal),
        taskService.list({}, controller.signal),
      ]);
      setProjects(projectList);
      setTasks(taskList);

      if (projectList.length === 0) {
        setStatusTotals(totalsFromTasks(taskList));
        setOverdueCount(
          taskList.filter((task) =>
            isOverdue(getField(task, "dueDate"), getField(task, "status")),
          ).length,
        );
      } else {
        const totals = { Todo: 0, InProgress: 0, Testing: 0, Done: 0 };
        let overdue = 0;
        const dashboards = await Promise.allSettled(
          projectList.map((project) =>
            dashboardService.getProjectDashboard(getField(project, "id")),
          ),
        );

        dashboards.forEach((result) => {
          if (result.status === "fulfilled" && result.value) {
            const counts = countFromDashboard(
              result.value.statusCounts || result.value.StatusCounts,
            );
            Object.keys(totals).forEach((key) => {
              totals[key] += counts[key];
            });
            overdue += Number(
              result.value.overdueTasks ?? result.value.OverdueTasks ?? 0,
            );
          }
        });

        const dashboardSucceeded = dashboards.some(
          (item) => item.status === "fulfilled" && item.value,
        );
        if (dashboardSucceeded) {
          setStatusTotals(totals);
          setOverdueCount(overdue);
        } else {
          setPartial(
            "Dashboard Service aggregates were unavailable. Task lists below still use Task Service data.",
          );
          setStatusTotals(totalsFromTasks(taskList));
          setOverdueCount(
            taskList.filter((task) =>
              isOverdue(getField(task, "dueDate"), getField(task, "status")),
            ).length,
          );
        }
      }

      const sprintSets = await Promise.allSettled(
        projectList.map((project) =>
          sprintService.listByProject(getField(project, "id")),
        ),
      );
      const sprints = sprintSets.flatMap((result) =>
        result.status === "fulfilled" ? result.value : [],
      );
      const progress = await Promise.allSettled(
        sprints.slice(0, 6).map((sprint) =>
          dashboardService.getSprintProgress(getField(sprint, "id")).then((data) => ({
            ...data,
            name: getField(sprint, "name"),
          })),
        ),
      );
      setSprintProgress(
        progress
          .filter((item) => item.status === "fulfilled")
          .map((item) => item.value),
      );
    } catch (err) {
      if (!isCancelError(err)) {
        setError(err.normalized?.message || "Unable to load dashboard data.");
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  const chartData = useMemo(() => {
    if (!statusTotals) return [];
    return TASK_STATUSES.map((status) => ({
      name: status.label,
      value: statusTotals[status.value] || 0,
      key: status.value,
    }));
  }, [statusTotals]);

  const overdueTasks = tasks.filter((task) =>
    isOverdue(getField(task, "dueDate"), getField(task, "status")),
  );
  const completed = statusTotals?.Done ?? tasks.filter((task) => getField(task, "status") === "Done").length;
  const totalTasks = statusTotals
    ? Object.values(statusTotals).reduce((sum, value) => sum + value, 0)
    : tasks.length;

  if (loading) {
    return (
      <div>
        <PageHeader title="Dashboard" subtitle="Workspace overview from live Gateway data." />
        <CardSkeletonGrid />
      </div>
    );
  }

  if (error) {
    return <ErrorState message={error} onRetry={load} />;
  }

  return (
    <div>
      <PageHeader
        title="Dashboard"
        subtitle="Figures come from the Dashboard Service when available, and from Task Service records for lists."
      />
      {partial ? (
        <p className="mb-4 rounded-lg bg-amber-50 p-3 text-sm text-amber-800">{partial}</p>
      ) : null}
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard title="Total projects" value={projects.length} />
        <StatCard title="Total tasks" value={totalTasks} />
        <StatCard title="Completed tasks" value={completed} />
        <StatCard
          title="Overdue tasks"
          value={overdueCount ?? overdueTasks.length}
        />
      </div>

      <div className="mt-6 grid gap-6 xl:grid-cols-5">
        <section className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900 xl:col-span-2">
          <h2 className="mb-4 text-base font-semibold">Task status</h2>
          {statusTotals ? (
            <>
              <div className="h-64" role="img" aria-label="Task status distribution">
                <ResponsiveContainer>
                  <PieChart>
                    <Pie data={chartData} dataKey="value" nameKey="name" innerRadius={60} outerRadius={90}>
                      {chartData.map((entry) => (
                        <Cell key={entry.key} fill={STATUS_COLORS[entry.key]} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
              </div>
              <ul className="mt-2 space-y-1 text-sm">
                {chartData.map((item) => (
                  <li key={item.key} className="flex justify-between">
                    <span>{item.name}</span>
                    <span>{item.value}</span>
                  </li>
                ))}
              </ul>
            </>
          ) : (
            <EmptyState title="Status chart unavailable" description="The Dashboard Service did not return status counts." />
          )}
        </section>

        <section className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900 xl:col-span-3">
          <h2 className="mb-4 text-base font-semibold">Sprint progress</h2>
          {sprintProgress.length === 0 ? (
            <EmptyState title="No sprint progress" description="Create a sprint, then dashboard progress will appear here." />
          ) : (
            <ul className="space-y-4">
              {sprintProgress.map((sprint) => {
                const percent = Number(
                  sprint.progressPercentage ?? sprint.ProgressPercentage ?? 0,
                );
                return (
                  <li key={sprint.sprintId || sprint.SprintId}>
                    <div className="mb-1 flex justify-between text-sm">
                      <span>{sprint.name}</span>
                      <span>{percent}%</span>
                    </div>
                    <div className="h-2 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
                      <div
                        className="h-full bg-indigo-600"
                        style={{ width: `${Math.min(percent, 100)}%` }}
                      />
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </section>
      </div>

      <section className="mt-6 rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900">
        <h2 className="mb-4 text-base font-semibold">Overdue work</h2>
        {overdueTasks.length === 0 ? (
          <EmptyState icon={FolderKanban} title="No overdue tasks" />
        ) : (
          <ul className="divide-y divide-slate-200 dark:divide-slate-800">
            {overdueTasks.slice(0, 8).map((task) => (
              <li key={getField(task, "id")} className="flex flex-wrap items-center justify-between gap-3 py-3">
                <Link className="font-medium hover:text-indigo-600" to={`/tasks/${getField(task, "id")}`}>
                  {getField(task, "title")}
                </Link>
                <div className="flex items-center gap-3 text-sm text-slate-500">
                  <StatusBadge status={getField(task, "status")} />
                  <span>{formatDate(getField(task, "dueDate"))}</span>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="mt-6">
        <h2 className="mb-3 text-base font-semibold">Projects</h2>
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {projects.map((project) => (
            <Link
              key={getField(project, "id")}
              to={`/projects/${getField(project, "id")}`}
              className="rounded-2xl border border-slate-200 bg-white p-4 hover:border-indigo-200 dark:border-slate-800 dark:bg-slate-900"
            >
              <h3 className="font-semibold">{getField(project, "name")}</h3>
              <p className="mt-1 line-clamp-2 text-sm text-slate-500">
                {getField(project, "description") || "No description"}
              </p>
            </Link>
          ))}
        </div>
      </section>
    </div>
  );
}

function StatCard({ title, value }) {
  return (
    <article className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900">
      <p className="text-sm text-slate-500">{title}</p>
      <p className="mt-3 text-3xl font-semibold">{value}</p>
    </article>
  );
}
