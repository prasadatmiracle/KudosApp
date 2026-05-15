import { Outlet, useLocation, Navigate } from "react-router-dom";
import { TopBar } from "./TopBar";
import { BottomNav } from "./BottomNav";
import { useAuth } from "@/lib/auth";

// SCR-1 + Assessment-1: titles reframed from surveillance ("Compliance",
// "Validation Queue") to engagement-oriented language. Same data, softer
// psychological framing per Assessment A5/A7.
const TITLES: Record<string, string> = {
  "/dashboard":    "Overview",
  "/health":       "Team pulse",
  "/heatmap":      "Participation calendar",   // was "Compliance Heatmap" — SCR-1 C2
  "/inbox":        "Smart inbox",
  "/tasks":        "Tasks & polls",
  "/daily":        "Daily check-in",            // softer than "Daily Update"
  "/feed":         "Activity feed",
  "/achievements": "Achievements",
  "/events":       "Team events",
  "/leaderboard":  "Recognition",               // less competitive than "Leaderboard"
  "/validation":   "Recognitions to review",    // softer than "Validation Queue"
  "/reports":      "Reports",
  "/profile":      "Profile",
};

export function AppShell() {
  const { isAuthenticated } = useAuth();
  const { pathname } = useLocation();

  if (!isAuthenticated) return <Navigate to="/login" replace />;

  const title = TITLES[pathname] ?? "KudosApp";

  return (
    <div className="min-h-dvh bg-background text-on-surface">
      <TopBar title={title} />
      <main className="mx-auto max-w-3xl px-4 pt-20 pb-32 animate-fade-in">
        <Outlet />
      </main>
      <BottomNav />
    </div>
  );
}
