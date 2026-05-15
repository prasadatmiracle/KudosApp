import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "http://localhost:5083",
        changeOrigin: true,
        secure: false,
      },
    },
  },
  build: {
    outDir: "../backend/KudosApp.Api/wwwroot-react",
    emptyOutDir: true,
  },
});
