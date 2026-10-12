import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import PageHeader from "../components/common/PageHeader";
import ProjectSubnav from "../components/layout/ProjectSubnav";
import ErrorState from "../components/common/ErrorState";
import Skeleton from "../components/common/Skeleton";
import StatusBadge from "../components/common/StatusBadge";
import projectService from "../services/projectService";
import taskService from "../services/taskService";
import sprintService from "../services/sprintService";
import teamService from "../services/teamService";
import dashboardService from "../services/dashboardService";
import authService from "../services/authService";
import { getField } from "../utils/http";
import { formatDate } from "../utils/format";

export default function ProjectOverview() {
  const { projectId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [project, setProject] = useState(null);
  const [tasks, setTasks] = useState([]);
  const [sprints, setSprints] = useState([]);
  const [members, setMembers] = useState([]);
  const [dashboard, setDashboard] = useState(null);
  const [usersById, setUsersById] = useState({});

  async function load() {
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
      const teamId = getField(projectData, "teamId");
      if (teamId) {
        setMembers(await teamService.getMembers(teamId));
      }
      try {
        setDashboard(await dashboardService.getProjectDashboard(projectId));
      } catch {
        setDashboard(null);
      }
    } catch (err) {
      setError(err.normalized?.message || "Unable to load project.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, [projectId]);

  if (loading) return <Skeleton className="h-64" />;
  if (error) return <ErrorState message={error} onRetry={load} />;

  const counts = dashboard?.statusCounts || dashboard?.StatusCounts || {};

  return (
    <div>
      <PageHeader
        crumbs="Projects"
        title={getField(project, "name")}
        subtitle={getField(project, "description") || "No description"}
      />
      <ProjectSubnav projectId={projectId} />
      <div className="grid gap-4 md:grid-cols-4">
        <Metric label="Total" value={dashboard?.totalTasks ?? tasks.length} />
        <Metric label="To Do" value={counts.todo ?? counts.Todo ?? "—"} />
        <Metric label="In Progress" value={counts.inProgress ?? counts.InProgress ?? "—"} />
        <Metric label="Done" value={counts.done ?? counts.Done ?? "—"} />
      </div>
      <div className="mt-6 grid gap-6 lg:grid-cols-2">
        <section className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900">
          <h2 className="mb-3 font-semibold">Recent tasks</h2>
          <ul className="space-y-3">
            {tasks.slice(0, 6).map((task) => (
              <li key={getField(task, "id")} className="flex items-center justify-between gap-3">
                <Link to={`/tasks/${getField(task, "id")}`}>{getField(task, "title")}</Link>
                <StatusBadge status={getField(task, "status")} />
              </li>
            ))}
          </ul>
        </section>
        <section className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900">
          <h2 className="mb-3 font-semibold">Sprints</h2>
          <ul className="space-y-3 text-sm">
            {sprints.map((sprint) => (
              <li key={getField(sprint, "id")}>
                <p className="font-medium">{getField(sprint, "name")}</p>
                <p className="text-slate-500">
                  {formatDate(getField(sprint, "startDate"))} –{" "}
                  {formatDate(getField(sprint, "endDate"))}
                </p>
              </li>
            ))}
          </ul>
        </section>
      </div>
      <section className="mt-6 rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900">
        <h2 className="mb-3 font-semibold">Members</h2>
        <ul className="grid gap-3 md:grid-cols-2">
          {members.map((member) => {
            const user = usersById[getField(member, "userId")];
            return (
              <li key={getField(member, "id")} className="text-sm">
                {user?.name || getField(member, "userId")} · {getField(member, "role")}
              </li>
            );
          })}
        </ul>
      </section>
    </div>
  );
}

function Metric({ label, value }) {
  return (
    <article className="rounded-2xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
      <p className="text-sm text-slate-500">{label}</p>
      <p className="mt-2 text-2xl font-semibold">{value}</p>
    </article>
  );
}
