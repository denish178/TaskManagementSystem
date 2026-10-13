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
import Avatar from "../components/common/Avatar";
import Field, { inputClassName } from "../components/common/Field";
import projectService from "../services/projectService";
import teamService from "../services/teamService";
import authService from "../services/authService";
import { useAuth } from "../context/AuthContext";
import { getField } from "../utils/http";

export default function ProjectMembers() {
  const { isAdmin } = useAuth();
  const { projectId } = useParams();
  const [project, setProject] = useState(null);
  const [members, setMembers] = useState([]);
  const [users, setUsers] = useState([]);
  const [usersById, setUsersById] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [addOpen, setAddOpen] = useState(false);
  const [removing, setRemoving] = useState(null);
  const [form, setForm] = useState({ userId: "", role: "Member" });
  const [saving, setSaving] = useState(false);

  const teamId = project ? getField(project, "teamId") : null;

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const projectData = await projectService.getById(projectId);
      setProject(projectData);
      const tid = getField(projectData, "teamId");
      const [memberList, userList] = await Promise.all([
        tid ? teamService.getMembers(tid) : Promise.resolve([]),
        authService.getUsers(),
      ]);
      setMembers(memberList);
      setUsers(userList);
      setUsersById(
        Object.fromEntries(userList.map((user) => [user.id || user.Id, user])),
      );
    } catch (err) {
      setError(err.normalized?.message || "Unable to load members.");
    } finally {
      setLoading(false);
    }
  }, [projectId]);

  useEffect(() => {
    load();
  }, [load]);

  async function addMember(event) {
    event.preventDefault();
    if (!form.userId || !teamId) return;
    setSaving(true);
    try {
      await teamService.addMember(teamId, {
        userId: form.userId,
        role: form.role || "Member",
      });
      toast.success("Member added");
      setAddOpen(false);
      setForm({ userId: "", role: "Member" });
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to add member.");
    } finally {
      setSaving(false);
    }
  }

  async function confirmRemove() {
    if (!teamId) return;
    setSaving(true);
    try {
      await teamService.removeMember(teamId, getField(removing, "userId"));
      toast.success("Member removed");
      setRemoving(null);
      await load();
    } catch (err) {
      toast.error(err.normalized?.message || "Unable to remove member.");
    } finally {
      setSaving(false);
    }
  }

  const memberUserIds = new Set(members.map((m) => getField(m, "userId")));
  const availableUsers = users.filter((user) => !memberUserIds.has(user.id || user.Id));

  if (loading) return <Skeleton className="h-64" />;
  if (error) return <ErrorState message={error} onRetry={load} />;

  return (
    <div>
      <PageHeader
        crumbs="Projects"
        title={getField(project, "name")}
        subtitle="Team members"
        actions={
          isAdmin ? (
            <Button onClick={() => setAddOpen(true)} disabled={!teamId}>
              Add member
            </Button>
          ) : null
        }
      />
      <ProjectSubnav projectId={projectId} />
      <ul className="divide-y divide-slate-200 rounded-2xl border border-slate-200 bg-white dark:divide-slate-800 dark:border-slate-800 dark:bg-slate-900">
        {members.map((member) => {
          const userId = getField(member, "userId");
          const user = usersById[userId];
          return (
            <li
              key={getField(member, "id")}
              className="flex flex-wrap items-center justify-between gap-3 p-4"
            >
              <div className="flex items-center gap-3">
                <Avatar name={user?.name} />
                <div>
                  <p className="font-medium">{user?.name || userId}</p>
                  <p className="text-sm text-slate-500">{user?.email}</p>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <span className="text-sm text-slate-500">{getField(member, "role")}</span>
                {isAdmin ? (
                  <Button variant="ghost" onClick={() => setRemoving(member)}>
                    Remove
                  </Button>
                ) : null}
              </div>
            </li>
          );
        })}
      </ul>

      <Modal open={addOpen} title="Add team member" onClose={() => setAddOpen(false)}>
        <form className="space-y-4" onSubmit={addMember}>
          <Field id="userId" label="User">
            <select
              id="userId"
              className={inputClassName}
              value={form.userId}
              onChange={(event) => setForm({ ...form, userId: event.target.value })}
              required
            >
              <option value="">Select user</option>
              {availableUsers.map((user) => (
                <option key={user.id || user.Id} value={user.id || user.Id}>
                  {user.name} ({user.email})
                </option>
              ))}
            </select>
          </Field>
          <Field id="role" label="Role">
            <input
              id="role"
              className={inputClassName}
              value={form.role}
              onChange={(event) => setForm({ ...form, role: event.target.value })}
            />
          </Field>
          <div className="flex justify-end">
            <Button type="submit" disabled={saving}>
              Add member
            </Button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        open={Boolean(removing)}
        title="Remove member"
        message="Remove this user from the project team?"
        confirmLabel="Remove"
        loading={saving}
        onClose={() => setRemoving(null)}
        onConfirm={confirmRemove}
      />
    </div>
  );
}
