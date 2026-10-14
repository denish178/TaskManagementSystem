export default function PageHeader({ title, subtitle, actions, crumbs }) {
  return (
    <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
      <div>
        {crumbs ? <p className="page-crumb">{crumbs}</p> : null}
        <h1 className="page-title">{title}</h1>
        {subtitle ? <p className="page-lead">{subtitle}</p> : null}
      </div>
      {actions ? <div className="flex flex-wrap gap-2">{actions}</div> : null}
    </div>
  );
}
