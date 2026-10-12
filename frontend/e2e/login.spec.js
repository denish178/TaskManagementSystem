import { test, expect } from "@playwright/test";

test.describe("authentication", () => {
  test("login page loads", async ({ page }) => {
    await page.goto("/login");
    await expect(page.getByRole("button", { name: /sign in/i })).toBeVisible();
  });

  test("admin can sign in and reach dashboard", async ({ page }) => {
    const password = process.env.TMS_DEV_ADMIN_PASSWORD;
    test.skip(!password, "Set TMS_DEV_ADMIN_PASSWORD (repo root .env) for this test.");

    await page.goto("/login");
    await page.getByLabel(/email/i).fill("admin@local.test");
    await page.getByLabel(/^password/i).fill(password);
    await page.getByRole("button", { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/dashboard/);
    await expect(page.getByText(/dashboard/i).first()).toBeVisible();
  });
});
