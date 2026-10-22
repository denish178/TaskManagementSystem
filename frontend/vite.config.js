import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: "./src/test/setup.js",
    exclude: ["**/node_modules/**", "**/dist/**", "e2e/**"],
    // forks pool often times out on Windows (e.g. OneDrive paths); threads is stable in CI and locally
    pool: "threads",
    testTimeout: 30_000,
  },
});
