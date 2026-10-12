import { Link } from "react-router-dom";
import { Calendar } from "lucide-react";
import StatusBadge from "../common/StatusBadge";
import PriorityBadge from "../common/PriorityBadge";
import { formatDate, shortId, isOverdue } from "../../utils/format";
import { getField } from "../../utils/http";

export default function TaskCard({ task, usersById = {}, dragHandleProps }) {
  const id = getField(task, "id");
  const title = getField(task, "title");
  const status = getField(task, "status");
  const priority = getField(task, "priority");
  const dueDate = getField(task, "dueDate");
  const assigneeId = getField(task, "assigneeId");
  const assignee = usersById[assigneeId];
  const overdue = isOverdue(dueDate, status);

  return (
    <article
      className="rounded-xl border border-slate-200 bg-white p-3 shadow-sm dark:border-slate-800 dark:bg-slate-900"
      {...dragHandleProps}
    >
      <div className="mb-2 flex items-start justify-between gap-2">
        <p className="text-[11px] font-semibold tracking-wide text-slate-400">
          {shortId(id)}
        </p>
        <PriorityBadge priority={priority} />
      </div>
      <Link
        to={`/tasks/${id}`}
        className="block text-sm font-semibold text-slate-900 hover:text-indigo-600 dark:text-white"
      >
        {title}
      </Link>
      <div className="mt-3 flex flex-wrap items-center gap-2 text-xs text-slate-500">
        <StatusBadge status={status} />
        {dueDate ? (
          <span className={overdue ? "flex items-center gap-1 text-rose-600" : "flex items-center gap-1"}>
            <Calendar size={12} />
            {formatDate(dueDate)}
          </span>
        ) : null}
        <span>{assignee?.name || "Unassigned"}</span>
      </div>
    </article>
  );
}
