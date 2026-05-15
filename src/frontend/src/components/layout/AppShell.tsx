import { Outlet, useLocation, Navigate } from "react-router-dom";
import { TopBar } from "./TopBar";
import { BottomNav } from "./BottomNav";
import { useAuth } from "@/lib/auth";

const TITLES: Record<string, string> = {
  "/dashboard":    "Dashboard",
  "/health":       "Team Health",
  "/heatmap":      "Compliance Heatmap",
  "/inbox":        "Smart Inbox",
  "/tasks":        "Tasks & Polls",
  "/daily":        "Daily Update",
  "/feed":         "Activity Feed",
  "/achievements": "Achievements",
  "/events":       "Events",
  "/leaderboard":  "Leaderboard",
  "/validation":   "Validation Queue",
  "/reports":      "Reports",
  "/profile":      "Profile",
};

export function AppShell() {
  const { isAuthenticated } = useAuth();
  const { pathname } = useLocation();

  if (!isAuthenticated) return <Navigate to="/login" replace />;

  const title = TITLES[pathname] ?? "KudosApp";

  return (
    <div className="min-h-dvh bg-background">
      <TopBar title={title} />
      <main className="mx-auto max-w-3xl px-4 py-5 pb-28 animate-fade-in">
        <Outlet />
      </main>
      <BottomNav />
    </div>
  );
}
