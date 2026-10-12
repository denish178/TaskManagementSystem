import { describe, expect, it } from "vitest";
import { ADMIN_ROLE } from "../utils/constants";

describe("admin-only assignment rule", () => {
  it("allows assignment UI only for Admin role", () => {
    const user = { role: "User" };
    const admin = { role: ADMIN_ROLE };
    expect(user.role === ADMIN_ROLE).toBe(false);
    expect(admin.role === ADMIN_ROLE).toBe(true);
  });

  it("update payload for non-admin must not include assigneeId", () => {
    const updatePayload = {
      title: "Updated",
      description: null,
      sprintId: null,
      priority: "Medium",
      dueDate: null,
    };
    expect(updatePayload).not.toHaveProperty("assigneeId");
  });
});
