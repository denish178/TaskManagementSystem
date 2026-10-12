import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import Field, { inputClassName } from "../common/Field";
import Button from "../common/Button";
import { TASK_PRIORITIES } from "../../utils/constants";
import { formatDateInput } from "../../utils/format";

const schema = z.object({
  title: z.string().trim().min(1, "Title is required").max(250),
  description: z.string().max(5000).optional().or(z.literal("")),
  priority: z.string().optional(),
  dueDate: z.string().optional(),
  sprintId: z.string().optional(),
  projectId: z.string().optional(),
});

export default function TaskForm({
  defaultValues,
  projects = [],
  sprints = [],
  onSubmit,
  onCancel,
  submitting,
  showProject = false,
}) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      title: defaultValues?.title || "",
      description: defaultValues?.description || "",
      priority: defaultValues?.priority || "Medium",
      dueDate: formatDateInput(defaultValues?.dueDate),
      sprintId: defaultValues?.sprintId || "",
      projectId: defaultValues?.projectId || "",
    },
  });

  return (
    <form
      className="space-y-4"
      onSubmit={handleSubmit((values) => onSubmit(values))}
    >
      {showProject ? (
        <Field id="projectId" label="Project" error={errors.projectId?.message}>
          <select id="projectId" className={inputClassName} {...register("projectId")}>
            <option value="">Select a project</option>
            {projects.map((project) => (
              <option key={project.id} value={project.id}>
                {project.name}
              </option>
            ))}
          </select>
        </Field>
      ) : null}
      <Field id="title" label="Title" error={errors.title?.message}>
        <input id="title" className={inputClassName} {...register("title")} />
      </Field>
      <Field id="description" label="Description" error={errors.description?.message}>
        <textarea
          id="description"
          rows={4}
          className={inputClassName}
          {...register("description")}
        />
      </Field>
      <div className="grid gap-4 sm:grid-cols-2">
        <Field id="priority" label="Priority">
          <select id="priority" className={inputClassName} {...register("priority")}>
            {TASK_PRIORITIES.map((item) => (
              <option key={item.value} value={item.value}>
                {item.label}
              </option>
            ))}
          </select>
        </Field>
        <Field id="dueDate" label="Due date">
          <input
            id="dueDate"
            type="date"
            className={inputClassName}
            {...register("dueDate")}
          />
        </Field>
      </div>
      <Field id="sprintId" label="Sprint">
        <select id="sprintId" className={inputClassName} {...register("sprintId")}>
          <option value="">No sprint</option>
          {sprints.map((sprint) => (
            <option key={sprint.id} value={sprint.id}>
              {sprint.name}
            </option>
          ))}
        </select>
      </Field>
      <p className="text-xs text-slate-500">
        Task assignment is available only to Admin users after the task is created.
      </p>
      <div className="flex justify-end gap-2">
        <Button variant="secondary" onClick={onCancel} disabled={submitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={submitting}>
          {submitting ? "Saving..." : "Save task"}
        </Button>
      </div>
    </form>
  );
}
