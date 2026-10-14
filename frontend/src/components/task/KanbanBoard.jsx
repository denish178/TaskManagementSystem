import { DragDropContext, Draggable, Droppable } from "@hello-pangea/dnd";
import { TASK_STATUSES } from "../../utils/constants";
import TaskCard from "./TaskCard";
import EmptyState from "../common/EmptyState";
import { getField } from "../../utils/http";

export default function KanbanBoard({
  tasks,
  usersById,
  onStatusChange,
  disabled,
}) {
  const grouped = TASK_STATUSES.reduce((acc, status) => {
    acc[status.value] = tasks.filter(
      (task) => getField(task, "status") === status.value,
    );
    return acc;
  }, {});

  function handleDragEnd(result) {
    if (!result.destination || disabled) return;
    const taskId = result.draggableId;
    const nextStatus = result.destination.droppableId;
    const task = tasks.find((item) => String(getField(item, "id")) === taskId);
    if (!task || getField(task, "status") === nextStatus) return;
    onStatusChange(taskId, nextStatus, getField(task, "status"));
  }

  return (
    <DragDropContext onDragEnd={handleDragEnd}>
      <div className="grid gap-4 xl:grid-cols-4">
        {TASK_STATUSES.map((column) => (
          <Droppable droppableId={column.value} key={column.value}>
            {(provided) => (
              <section
                ref={provided.innerRef}
                {...provided.droppableProps}
                className="kanban-column"
              >
                <div className="mb-3 flex items-center justify-between px-1">
                  <h2 className="text-sm font-semibold">{column.label}</h2>
                  <span className="rounded-full border border-[var(--app-border)] bg-[var(--app-surface)] px-2 py-0.5 text-xs text-[var(--app-text-muted)]">
                    {grouped[column.value].length}
                  </span>
                </div>
                <div className="space-y-3">
                  {grouped[column.value].map((task, index) => {
                    const id = String(getField(task, "id"));
                    return (
                      <Draggable draggableId={id} index={index} key={id}>
                        {(drag) => (
                          <div
                            ref={drag.innerRef}
                            {...drag.draggableProps}
                            {...drag.dragHandleProps}
                          >
                            <TaskCard task={task} usersById={usersById} />
                          </div>
                        )}
                      </Draggable>
                    );
                  })}
                  {provided.placeholder}
                  {grouped[column.value].length === 0 ? (
                    <EmptyState title="No tasks" description="Drop a card here or create a task." />
                  ) : null}
                </div>
              </section>
            )}
          </Droppable>
        ))}
      </div>
    </DragDropContext>
  );
}
