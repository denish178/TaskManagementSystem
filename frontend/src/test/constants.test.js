import { describe, expect, it } from "vitest";
import { ADMIN_ROLE, TASK_STATUSES } from "../utils/constants";

describe("task constants", () => {
  it("defines four kanban statuses", () => {
    expect(TASK_STATUSES.map((s) => s.value)).toEqual([
      "Todo",
      "InProgress",
      "Testing",
      "Done",
    ]);
  });

  it("uses Admin role name from backend", () => {
    expect(ADMIN_ROLE).toBe("Admin");
  });
});
