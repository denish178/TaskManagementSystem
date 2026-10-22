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

const userOnlyPaths = new Set(["/my-workspace", "/my-tasks"]);

export default function Sidebar({ collapsed, onToggle, mobileOpen, onClose }) {
  const { user, isAdmin } = useAuth();
  const { projectId } = useParams();
  const navItems = isAdmin
    ? items.filter((item) => !userOnlyPaths.has(item.to))
    : items;

  return (
    <>
      {mobileOpen ? (
        <button
          type="button"
          className="fixed inset-0 z-30 bg-stone-900/40 lg:hidden"
          aria-label="Close navigation"
          onClick={onClose}
        />
      ) : null}
      <aside
        className={`sidebar fixed inset-y-0 left-0 z-40 flex flex-col transition-all ${
          collapsed ? "w-[76px]" : "w-64"
        } ${mobileOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0"}`}
      >
        <div className="flex items-center justify-between px-4 py-5">
          <div className="flex items-center gap-3 overflow-hidden">
            <span className="logo-mark h-9 w-9 shrink-0 text-sm">T</span>
            {!collapsed ? (
              <span className="truncate text-base font-semibold tracking-tight">TaskFlow</span>
            ) : null}
          </div>
          <button
            type="button"
            className="icon-btn hidden lg:inline-flex"
            onClick={onToggle}
            aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
          >
            {collapsed ? <PanelLeftOpen size={18} /> : <PanelLeftClose size={18} />}
          </button>
        </div>

        <nav className="flex-1 space-y-0.5 px-3">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              onClick={onClose}
              className={({ isActive }) =>
                `nav-item ${isActive ? "nav-item-active" : ""}`
              }
            >
              <item.icon size={18} strokeWidth={1.75} />
              {!collapsed ? item.label : <span className="sr-only">{item.label}</span>}
            </NavLink>
          ))}

          {projectId ? (
            <div className="pt-4">
              {!collapsed ? (
                <p className="px-3 pb-2 text-xs font-medium text-[var(--app-text-muted)]">
                  This project
                </p>
              ) : null}
              <NavLink
                to={`/projects/${projectId}/members`}
                onClick={onClose}
                className={({ isActive }) =>
                  `nav-item ${isActive ? "nav-item-active" : ""}`
                }
              >
                <Users size={18} strokeWidth={1.75} />
                {!collapsed ? "Members" : <span className="sr-only">Members</span>}
              </NavLink>
            </div>
          ) : null}
        </nav>

        <div className="border-t border-[var(--app-border)] p-4">
          <div className="flex items-center gap-3">
            <Avatar name={user?.name} />
            {!collapsed ? (
              <div className="min-w-0">
                <p className="truncate text-sm font-semibold">{user?.name}</p>
                <p className="truncate text-xs text-[var(--app-text-muted)]">{user?.role}</p>
              </div>
            ) : null}
          </div>
        </div>
      </aside>
    </>
  );
}
