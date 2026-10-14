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
    <div className="mb-6 flex flex-wrap gap-1 border-b border-[var(--app-border)] pb-3">
      {links.map((link) => (
        <NavLink
          key={link.to}
          to={link.to}
          end={link.end}
          className={({ isActive }) =>
            `subnav-link ${isActive ? "subnav-link-active" : ""}`
          }
        >
          {link.label}
        </NavLink>
      ))}
    </div>
  );
}
