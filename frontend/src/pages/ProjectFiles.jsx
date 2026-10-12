import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import PageHeader from "../components/common/PageHeader";
import ProjectSubnav from "../components/layout/ProjectSubnav";
import ErrorState from "../components/common/ErrorState";
import Skeleton from "../components/common/Skeleton";
import EmptyState from "../components/common/EmptyState";
import projectService from "../services/projectService";
import taskService from "../services/taskService";
import fileService from "../services/fileService";
import { getField } from "../utils/http";
import { formatBytes, formatDateTime } from "../utils/format";
import { Paperclip } from "lucide-react";

export default function ProjectFiles() {
  const { projectId } = useParams();
  const [project, setProject] = useState(null);
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const [projectData, tasks] = await Promise.all([
        projectService.getById(projectId),
        taskService.list({ projectId }),
      ]);
      setProject(projectData);
      const fileSets = await Promise.allSettled(
        tasks.map(async (task) => {
          const taskId = getField(task, "id");
          const files = await fileService.listByTask(taskId);
          return files.map((file) => ({
            ...file,
            taskTitle: getField(task, "title"),
            taskId,
          }));
        }),
      );
      setRows(
        fileSets.flatMap((result) => (result.status === "fulfilled" ? result.value : [])),
      );
    } catch (err) {
      setError(err.normalized?.message || "Unable to load project files.");
    } finally {
      setLoading(false);
    }
  }, [projectId]);

  useEffect(() => {
    load();
  }, [load]);

  if (loading) return <Skeleton className="h-64" />;
  if (error) return <ErrorState message={error} onRetry={load} />;

  return (
    <div>
      <PageHeader
        crumbs="Projects"
        title={getField(project, "name")}
        subtitle="Files attached to tasks in this project"
      />
      <ProjectSubnav projectId={projectId} />
      {rows.length === 0 ? (
        <EmptyState
          icon={Paperclip}
          title="No files yet"
          description="Upload attachments from a task details page."
        />
      ) : (
        <ul className="divide-y divide-slate-200 rounded-2xl border border-slate-200 bg-white dark:divide-slate-800 dark:border-slate-800 dark:bg-slate-900">
          {rows.map((file) => (
            <li
              key={getField(file, "id")}
              className="flex flex-wrap items-center justify-between gap-3 p-4 text-sm"
            >
              <div>
                <p className="font-medium">{getField(file, "fileName")}</p>
                <p className="text-slate-500">
                  <Link to={`/tasks/${file.taskId}`} className="text-indigo-600">
                    {file.taskTitle}
                  </Link>
                  {" · "}
                  {formatBytes(getField(file, "fileSize"))} ·{" "}
                  {formatDateTime(getField(file, "uploadedAt"))}
                </p>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
