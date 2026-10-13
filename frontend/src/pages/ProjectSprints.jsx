import { useCallback, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { toast } from "react-toastify";
import PageHeader from "../components/common/PageHeader";
import ProjectSubnav from "../components/layout/ProjectSubnav";
import ErrorState from "../components/common/ErrorState";
import Skeleton from "../components/common/Skeleton";
import Button from "../components/common/Button";
import Modal from "../components/common/Modal";
import ConfirmDialog from "../components/common/ConfirmDialog";
import Field, { inputClassName } from "../components/common/Field";
import projectService from "../services/projectService";
import sprintService from "../services/sprintService";
import dashboardService from "../services/dashboardService";
import { useAuth } from "../context/AuthContext";
import { getField } from "../utils/http";
import { formatDate, formatDateInput } from "../utils/format";

const emptyForm = {
  name: "",
  goal: "",
  startDate: "",
  endDate: "",
};

export default function ProjectSprints() {
  const { isAdmin } = useAuth();
  const { projectId } = useParams();
  const [project, setProject] = useState(null);
  const [sprints, setSprints] = useState([]);
  const [progressById, setProgressById] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState(null);
  const [deleting, setDeleting] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const [projectData, sprintList] = await Promise.all([
        projectService.getById(projectId),
        sprintService.listByProject(projectId),
      ]);
      setProject(projectData);
      setSprints(sprintList);
      const progressEntries = await Promise.allSettled(
        sprintList.map(async (sprint) => {
          const id = getField(sprint, "id");
          const data = await dashboardService.getSprintProgress(id);
          return [id, data];
        }),
      );
      setProgressById(
        Object.fromEntries(
          progressEntries
            .filter((item) => item.status === "fulfilled")
            .map((item) => item.value),
        ),
      );
    } catch (err) {
      setError(err.normalized?.message || "Unable to load sprints.");
    } finally {
      setLoading(false);
    }
  }, [projectId]);

  useEffect(() => {
    load();
  }, [load]);

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setFormOpen(true);
  }

  function openEdit(sprint) {
    setEditing(sprint);
    setForm({
      name: getField(sprint, "name") || "",
      goal: getField(sprint, "goal") || "",
      startDate: formatDateInput(getField(sprint, "startDate")),
      endDate: formatDateInput(getField(sprint, "endDate")),
    });
    setFormOpen(true);
  }

  async function save(event) {
    event.preventDefault();
    setSaving(true);
    try {
      const payload = {
        name: form.name.trim(),
        goal: form.goal || null,
        startDate: new Date(`${form.startDate}T00:00:00`).toISOString(),
        endDate: new Date(`${form.endDate}T00:00:00`).toISOString(),
      };
      if (editing) {
        await sprintService.update(getField(editing, "id"), payload);
        toast.success("Sprint updated");
      } else {
        await sprintService.create({ ...payload, projectId });
        toast.success("Sprint created");
      }
      setFormOpen(false);
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to save sprint.");
    } finally {
      setSaving(false);
    }
  }

  async function confirmDelete() {
    setSaving(true);
    try {
      await sprintService.remove(getField(deleting, "id"));
      toast.success("Sprint deleted");
      setDeleting(null);
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to delete sprint.");
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <Skeleton className="h-64" />;
  if (error) return <ErrorState message={error} onRetry={load} />;

  return (
    <div>
      <PageHeader
        crumbs="Projects"
        title={getField(project, "name")}
        subtitle="Sprints"
        actions={isAdmin ? <Button onClick={openCreate}>New sprint</Button> : null}
      />
      <ProjectSubnav projectId={projectId} />
      <ul className="space-y-4">
        {sprints.map((sprint) => {
          const id = getField(sprint, "id");
          const progress = progressById[id];
          const percent = Number(
            progress?.progressPercentage ?? progress?.ProgressPercentage ?? 0,
          );
          return (
            <li
              key={id}
              className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900"
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h2 className="text-lg font-semibold">{getField(sprint, "name")}</h2>
                  <p className="mt-1 text-sm text-slate-500">
                    {formatDate(getField(sprint, "startDate"))} –{" "}
                    {formatDate(getField(sprint, "endDate"))}
                  </p>
                  {getField(sprint, "goal") ? (
                    <p className="mt-2 text-sm">{getField(sprint, "goal")}</p>
                  ) : null}
                </div>
                <div className="flex gap-2">
                  {isAdmin ? (
                    <>
                      <Button variant="secondary" onClick={() => openEdit(sprint)}>
                        Edit
                      </Button>
                      <Button variant="ghost" onClick={() => setDeleting(sprint)}>
                        Delete
                      </Button>
                    </>
                  ) : null}
                </div>
              </div>
              {progress ? (
                <div className="mt-4">
                  <div className="mb-1 flex justify-between text-xs text-slate-500">
                    <span>
                      {progress.completedTasks ?? progress.CompletedTasks ?? 0} /{" "}
                      {progress.totalTasks ?? progress.TotalTasks ?? 0} done
                    </span>
                    <span>{percent}%</span>
                  </div>
                  <div className="h-2 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
                    <div
                      className="h-full bg-indigo-600"
                      style={{ width: `${Math.min(percent, 100)}%` }}
                    />
                  </div>
                </div>
              ) : null}
            </li>
          );
        })}
      </ul>

      <Modal
        open={formOpen}
        title={editing ? "Edit sprint" : "Create sprint"}
        onClose={() => setFormOpen(false)}
      >
        <form className="space-y-4" onSubmit={save}>
          <Field id="name" label="Name">
            <input
              id="name"
              className={inputClassName}
              value={form.name}
              onChange={(event) => setForm({ ...form, name: event.target.value })}
              required
            />
          </Field>
          <Field id="goal" label="Goal">
            <textarea
              id="goal"
              className={inputClassName}
              rows={3}
              value={form.goal}
              onChange={(event) => setForm({ ...form, goal: event.target.value })}
            />
          </Field>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field id="startDate" label="Start date">
              <input
                id="startDate"
                type="date"
                className={inputClassName}
                value={form.startDate}
                onChange={(event) =>
                  setForm({ ...form, startDate: event.target.value })
                }
                required
              />
            </Field>
            <Field id="endDate" label="End date">
              <input
                id="endDate"
                type="date"
                className={inputClassName}
                value={form.endDate}
                onChange={(event) =>
                  setForm({ ...form, endDate: event.target.value })
                }
                required
              />
            </Field>
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={() => setFormOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={saving}>
              {saving ? "Saving..." : "Save"}
            </Button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        open={Boolean(deleting)}
        title="Delete sprint"
        message="Delete this sprint from the project?"
        confirmLabel="Delete"
        loading={saving}
        onClose={() => setDeleting(null)}
        onConfirm={confirmDelete}
      />
    </div>
  );
}
