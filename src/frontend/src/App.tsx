import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { Toaster } from "sonner";
import { AuthProvider } from "@/lib/auth";
import { ThemeProvider } from "@/lib/theme";
import { AppShell } from "@/components/layout/AppShell";
import { Login } from "@/pages/Login";
import { Dashboard } from "@/pages/Dashboard";
import { Daily } from "@/pages/Daily";
import { Feed } from "@/pages/Feed";
import { Profile } from "@/pages/Profile";
import { Leaderboard } from "@/pages/Leaderboard";
import { Achievements } from "@/pages/Achievements";
import { Health } from "@/pages/Health";
import { Heatmap } from "@/pages/Heatmap";
import { InboxPage } from "@/pages/InboxPage";
import { Validation } from "@/pages/Validation";
import { Reports } from "@/pages/Reports";
import { Tasks, Events } from "@/pages/Stubs";

const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 30_000, retry: 1, refetchOnWindowFocus: false } },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider defaultTheme="dark">
        <AuthProvider>
          <BrowserRouter>
            <Routes>
              <Route path="/login" element={<Login />} />
              <Route element={<AppShell />}>
                <Route index               element={<Navigate to="/dashboard" replace />} />
                <Route path="dashboard"    element={<Dashboard />} />
                <Route path="daily"        element={<Daily />} />
                <Route path="feed"         element={<Feed />} />
                <Route path="profile"      element={<Profile />} />
                <Route path="leaderboard"  element={<Leaderboard />} />
                <Route path="achievements" element={<Achievements />} />
                <Route path="health"       element={<Health />} />
                <Route path="heatmap"      element={<Heatmap />} />
                <Route path="inbox"        element={<InboxPage />} />
                <Route path="tasks"        element={<Tasks />} />
                <Route path="events"       element={<Events />} />
                <Route path="validation"   element={<Validation />} />
                <Route path="reports"      element={<Reports />} />
              </Route>
              <Route path="*" element={<Navigate to="/dashboard" replace />} />
            </Routes>
          </BrowserRouter>
          <Toaster position="top-center" richColors closeButton theme="system" />
        </AuthProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
