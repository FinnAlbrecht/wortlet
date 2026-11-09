/// <reference types="vitest/config" />
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Im Entwicklungsmodus leitet Vite alle /api-Aufrufe an das .NET-Backend weiter.
const backend = process.env.WORTLET_API ?? "http://localhost:5080";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": { target: backend, changeOrigin: true },
      "/health": { target: backend, changeOrigin: true },
    },
  },
  build: {
    outDir: "dist",
    sourcemap: false,
  },
  test: {
    include: ["src/**/*.test.ts"],
    environment: "node",
  },
});
