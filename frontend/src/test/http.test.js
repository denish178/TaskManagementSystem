import { describe, expect, it } from "vitest";
import { asArray, getField, normalizeError } from "../utils/http";

describe("http utils", () => {
  it("normalizes array responses", () => {
    expect(asArray([1, 2])).toEqual([1, 2]);
    expect(asArray({ $values: [3] })).toEqual([3]);
    expect(asArray(null)).toEqual([]);
  });

  it("reads camelCase and PascalCase fields", () => {
    expect(getField({ status: "Todo" }, "status")).toBe("Todo");
    expect(getField({ Status: "Done" }, "status")).toBe("Done");
  });

  it("normalizes API errors", () => {
    const error = normalizeError({
      response: { status: 400, data: { message: "Invalid status." } },
    });
    expect(error.message).toBe("Invalid status.");
    expect(error.status).toBe(400);
  });
});
