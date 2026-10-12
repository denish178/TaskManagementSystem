import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { toast } from "react-toastify";
import { FolderKanban } from "lucide-react";
import PageHeader from "../components/common/PageHeader";
import Button from "../components/common/Button";
import EmptyState from "../components/common/EmptyState";
import ErrorState from "../components/common/ErrorState";
import Skeleton from "../components/common/Skeleton";
import Modal from "../components/common/Modal";
import ConfirmDialog from "../components/common/ConfirmDialog";
import Field, { inputClassName } from "../components/common/Field";
import projectService from "../services/projectService";
import teamService from "../services/teamService";
import { useAuth } from "../context/AuthContext";
import { getField } from "../utils/http";
import { formatDate } from "../utils/format";
import useDebounce from "../hooks/useDebounce";

export default function Projects() {
  const { user } = useAuth();
  const [projects, setProjects] = useState([]);
  const [teams, setTeams] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [query, setQuery] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState(null);
  const [deleting, setDeleting] = useState(null);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({ name: "", description: "", teamId: "" });
  const [teamFormOpen, setTeamFormOpen] = useState(false);
  const [teamName, setTeamName] = useState("");
  const [editingTeam, setEditingTeam] = useState(null);
  const [deletingTeam, setDeletingTeam] = useState(null);
  const [teamEditForm, setTeamEditForm] = useState({ name: "", description: "" });
  const debounced = useDebounce(query);

  async function load() {
    setLoading(true);
    setError("");
    try {
      const [projectList, teamList] = await Promise.all([
        projectService.list(),
        teamService.list(),
      ]);
      setProjects(projectList);
      setTeams(teamList);
    } catch (err) {
      setError(err.normalized?.message || "Unable to load projects.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  const filtered = useMemo(() => {
    const needle = debounced.trim().toLowerCase();
    if (!needle) return projects;
    return projects.filter((project) =>
      `${getField(project, "name")} ${getField(project, "description") || ""}`
        .toLowerCase()
        .includes(needle),
    );
  }, [projects, debounced]);

  function openCreate() {
    setEditing(null);
    setForm({ name: "", description: "", teamId: teams[0]?.id || "" });
    setFormOpen(true);
  }

  function openEdit(project) {
    setEditing(project);
    setForm({
      name: getField(project, "name") || "",
      description: getField(project, "description") || "",
      teamId: getField(project, "teamId") || "",
    });
    setFormOpen(true);
  }

  async function save(event) {
    event.preventDefault();
    if (!form.name.trim()) return;
    setSaving(true);
    try {
      if (editing) {
        await projectService.update(getField(editing, "id"), {
          name: form.name.trim(),
          description: form.description,
        });
        toast.success("Project updated");
      } else {
        if (!form.teamId) {
          toast.error("Create a team before adding a project.");
          return;
        }
        await projectService.create({
          teamId: form.teamId,
          name: form.name.trim(),
          description: form.description,
          createdBy: user.id,
        });
        toast.success("Project created");
      }
      setFormOpen(false);
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to save project.");
    } finally {
      setSaving(false);
    }
  }

  async function confirmDelete() {
    setSaving(true);
    try {
      await projectService.remove(getField(deleting, "id"));
      toast.success("Project deleted");
      setDeleting(null);
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to delete project.");
    } finally {
      setSaving(false);
    }
  }

  function openEditTeam(team) {
    setEditingTeam(team);
    setTeamEditForm({
      name: getField(team, "name") || "",
      description: getField(team, "description") || "",
    });
  }

  async function saveTeam(event) {
    event.preventDefault();
    if (!editingTeam || !teamEditForm.name.trim()) return;
    setSaving(true);
    try {
      await teamService.update(getField(editingTeam, "id"), {
        name: teamEditForm.name.trim(),
        description: teamEditForm.description || null,
      });
      toast.success("Team updated");
      setEditingTeam(null);
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to update team.");
    } finally {
      setSaving(false);
    }
  }

  async function confirmDeleteTeam() {
    if (!deletingTeam) return;
    setSaving(true);
    try {
      await teamService.remove(getField(deletingTeam, "id"));
      toast.success("Team deleted");
      setDeletingTeam(null);
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to delete team.");
    } finally {
      setSaving(false);
    }
  }

  async function createTeam(event) {
    event.preventDefault();
    if (!teamName.trim()) return;
    setSaving(true);
    try {
      const team = await teamService.create({
        name: teamName.trim(),
        description: "",
        createdBy: user.id,
      });
      toast.success("Team created");
      setTeamFormOpen(false);
      setTeamName("");
      await load();
      setForm((current) => ({ ...current, teamId: team.id || team.Id }));
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to create team.");
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return (
      <div className="grid gap-4 md:grid-cols-2">
        <Skeleton className="h-36" />
        <Skeleton className="h-36" />
      </div>
    );
  }

  if (error) return <ErrorState message={error} onRetry={load} />;

  return (
    <div>
      <PageHeader
        title="Projects"
        subtitle="Projects belong to a team in the Project Service."
        actions={
          <>
            <Button variant="secondary" onClick={() => setTeamFormOpen(true)}>
              New team
            </Button>
            <Button onClick={openCreate}>New project</Button>
          </>
        }
      />
      {teams.length > 0 ? (
        <section className="mb-6 rounded-2xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
          <h2 className="text-sm font-semibold text-slate-700 dark:text-slate-200">Your teams</h2>
          <ul className="mt-3 flex flex-wrap gap-2">
            {teams.map((team) => (
              <li
                key={getField(team, "id")}
                className="flex items-center gap-2 rounded-xl border border-slate-200 px-3 py-2 text-sm dark:border-slate-700"
              >
                <span className="font-medium">{getField(team, "name")}</span>
                <Button variant="ghost" onClick={() => openEditTeam(team)}>
                  Edit
                </Button>
                <Button variant="ghost" onClick={() => setDeletingTeam(team)}>
                  Delete
                </Button>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <input
        value={query}
        onChange={(event) => setQuery(event.target.value)}
        placeholder="Filter projects"
        className={`${inputClassName} mb-5 max-w-md`}
        aria-label="Filter projects"
      />
      {filtered.length === 0 ? (
        <EmptyState
          icon={FolderKanban}
          title="No projects"
          description="Create a team, then add a project."
          action={<Button onClick={openCreate}>Create project</Button>}
        />
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {filtered.map((project) => (
            <article
              key={getField(project, "id")}
              className="rounded-2xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900"
            >
              <h2 className="text-lg font-semibold">
                <Link to={`/projects/${getField(project, "id")}`}>
                  {getField(project, "name")}
                </Link>
              </h2>
              <p className="mt-2 line-clamp-3 text-sm text-slate-500">
                {getField(project, "description") || "No description"}
              </p>
              <p className="mt-3 text-xs text-slate-400">
                Created {formatDate(getField(project, "createdAt"))} ·{" "}
                {getField(project, "sprintCount") ?? 0} sprints
              </p>
              <div className="mt-4 flex gap-2">
                <Button variant="secondary" onClick={() => openEdit(project)}>
                  Edit
                </Button>
                <Button variant="ghost" onClick={() => setDeleting(project)}>
                  Delete
                </Button>
              </div>
            </article>
          ))}
        </div>
      )}

      <Modal
        open={formOpen}
        title={editing ? "Edit project" : "Create project"}
        onClose={() => setFormOpen(false)}
      >
        <form className="space-y-4" onSubmit={save}>
          {!editing ? (
            <Field id="teamId" label="Team">
              <select
                id="teamId"
                className={inputClassName}
                value={form.teamId}
                onChange={(event) => setForm({ ...form, teamId: event.target.value })}
              >
                <option value="">Select team</option>
                {teams.map((team) => (
                  <option key={getField(team, "id")} value={getField(team, "id")}>
                    {getField(team, "name")}
                  </option>
                ))}
              </select>
            </Field>
          ) : null}
          <Field id="name" label="Name">
            <input
              id="name"
              className={inputClassName}
              value={form.name}
              onChange={(event) => setForm({ ...form, name: event.target.value })}
              required
            />
          </Field>
          <Field id="description" label="Description">
            <textarea
              id="description"
              className={inputClassName}
              rows={3}
              value={form.description}
              onChange={(event) =>
                setForm({ ...form, description: event.target.value })
              }
            />
          </Field>
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

      <Modal open={teamFormOpen} title="Create team" onClose={() => setTeamFormOpen(false)}>
        <form className="space-y-4" onSubmit={createTeam}>
          <Field id="teamName" label="Team name">
            <input
              id="teamName"
              className={inputClassName}
              value={teamName}
              onChange={(event) => setTeamName(event.target.value)}
              required
            />
          </Field>
          <div className="flex justify-end">
            <Button type="submit" disabled={saving}>
              Create team
            </Button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        open={Boolean(deleting)}
        title="Delete project"
        message="This removes the project from the Project Service. This cannot be undone."
        confirmLabel="Delete"
        loading={saving}
        onClose={() => setDeleting(null)}
        onConfirm={confirmDelete}
      />

      <Modal
        open={Boolean(editingTeam)}
        title="Edit team"
        onClose={() => setEditingTeam(null)}
      >
        <form className="space-y-4" onSubmit={saveTeam}>
          <Field id="editTeamName" label="Team name">
            <input
              id="editTeamName"
              className={inputClassName}
              value={teamEditForm.name}
              onChange={(event) =>
                setTeamEditForm({ ...teamEditForm, name: event.target.value })
              }
              required
            />
          </Field>
          <Field id="editTeamDescription" label="Description">
            <textarea
              id="editTeamDescription"
              className={inputClassName}
              rows={2}
              value={teamEditForm.description}
              onChange={(event) =>
                setTeamEditForm({ ...teamEditForm, description: event.target.value })
              }
            />
          </Field>
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={() => setEditingTeam(null)}>
              Cancel
            </Button>
            <Button type="submit" disabled={saving}>
              Save team
            </Button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        open={Boolean(deletingTeam)}
        title="Delete team"
        message="Delete this team? Projects under it may fail if they still reference it."
        confirmLabel="Delete"
        loading={saving}
        onClose={() => setDeletingTeam(null)}
        onConfirm={confirmDeleteTeam}
      />
    </div>
  );
}
