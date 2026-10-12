import { NavLink } from "react-router-dom";

export default function ProjectSubnav({ projectId }) {
  const links = [
    { to: `/projects/${projectId}`, label: "Overview", end: true },
    { to: `/projects/${projectId}/tasks`, label: "Board" },
    { to: `/projects/${projectId}/sprints`, label: "Sprints" },
    { to: `/projects/${projectId}/members`, label: "Members" },
    { to: `/projects/${projectId}/files`, label: "Files" },
  ];

  return (
    <div className="mb-6 flex flex-wrap gap-2 border-b border-slate-200 pb-3 dark:border-slate-800">
      {links.map((link) => (
        <NavLink
          key={link.to}
          to={link.to}
          end={link.end}
          className={({ isActive }) =>
            `rounded-lg px-3 py-1.5 text-sm font-medium ${
              isActive
                ? "bg-indigo-50 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-200"
                : "text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-900"
            }`
          }
        >
          {link.label}
        </NavLink>
      ))}
    </div>
  );
}
