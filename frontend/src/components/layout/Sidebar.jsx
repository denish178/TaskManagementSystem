import { NavLink, useParams } from "react-router-dom";
import {
  FolderKanban,
  LayoutDashboard,
  ListTodo,
  PanelLeftClose,
  PanelLeftOpen,
  SquareUser,
  UserRound,
  Users,
} from "lucide-react";
import { useAuth } from "../../context/AuthContext";
import Avatar from "../common/Avatar";

const items = [
  { to: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { to: "/projects", label: "Projects", icon: FolderKanban },
  { to: "/my-workspace", label: "My Workspace", icon: SquareUser },
  { to: "/my-tasks", label: "My Tasks", icon: ListTodo },
  { to: "/profile", label: "Profile", icon: UserRound },
];

export default function Sidebar({ collapsed, onToggle, mobileOpen, onClose }) {
  const { user } = useAuth();
  const { projectId } = useParams();

  return (
    <>
      {mobileOpen ? (
        <button
          type="button"
          className="fixed inset-0 z-30 bg-slate-950/40 lg:hidden"
          aria-label="Close navigation"
          onClick={onClose}
        />
      ) : null}
      <aside
        className={`fixed inset-y-0 left-0 z-40 flex flex-col border-r border-slate-200 bg-white transition-all dark:border-slate-800 dark:bg-slate-950 ${
          collapsed ? "w-[76px]" : "w-64"
        } ${mobileOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0"}`}
      >
        <div className="flex items-center justify-between px-4 py-5">
          <div className="flex items-center gap-3 overflow-hidden">
            <span className="grid h-9 w-9 shrink-0 place-items-center rounded-xl bg-indigo-600 font-bold text-white">
              T
            </span>
            {!collapsed ? (
              <span className="truncate text-lg font-semibold">TaskFlow</span>
            ) : null}
          </div>
          <button
            type="button"
            className="hidden rounded-md p-1 text-slate-500 hover:bg-slate-100 lg:block dark:hover:bg-slate-800"
            onClick={onToggle}
            aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
          >
            {collapsed ? <PanelLeftOpen size={18} /> : <PanelLeftClose size={18} />}
          </button>
        </div>

        <nav className="flex-1 space-y-1 px-3">
          {items.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              onClick={onClose}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium ${
                  isActive
                    ? "bg-indigo-50 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-200"
                    : "text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-900"
                }`
              }
            >
              <item.icon size={18} />
              {!collapsed ? item.label : <span className="sr-only">{item.label}</span>}
            </NavLink>
          ))}

          {projectId ? (
            <div className="pt-4">
              {!collapsed ? (
                <p className="px-3 pb-2 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
                  Project
                </p>
              ) : null}
              <NavLink
                to={`/projects/${projectId}/members`}
                onClick={onClose}
                className="flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-900"
              >
                <Users size={18} />
                {!collapsed ? "Members" : <span className="sr-only">Members</span>}
              </NavLink>
            </div>
          ) : null}
        </nav>

        <div className="border-t border-slate-200 p-4 dark:border-slate-800">
          <div className="flex items-center gap-3">
            <Avatar name={user?.name} />
            {!collapsed ? (
              <div className="min-w-0">
                <p className="truncate text-sm font-semibold">{user?.name}</p>
                <p className="truncate text-xs text-slate-500">{user?.role}</p>
              </div>
            ) : null}
          </div>
        </div>
      </aside>
    </>
  );
}
