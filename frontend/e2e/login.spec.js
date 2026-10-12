import { test, expect } from "@playwright/test";

test.describe("authentication", () => {
  test("login page loads", async ({ page }) => {
    await page.goto("/login");
    await expect(page.getByRole("button", { name: /sign in/i })).toBeVisible();
  });

  test("admin can sign in and reach dashboard", async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel(/email/i).fill("admin@local.test");
    await page.getByLabel(/^password/i).fill("Admin1234!");
    await page.getByRole("button", { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/dashboard/);
    await expect(page.getByText(/dashboard/i).first()).toBeVisible();
  });
});
