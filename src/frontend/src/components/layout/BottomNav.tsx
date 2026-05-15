import { NavLink, useLocation } from "react-router-dom";
import {
  LayoutDashboard, Activity, CalendarDays, Inbox, ListChecks, PenLine,
  Rss, Trophy, PartyPopper, BarChart3, CheckCircle2, FileText, User,
} from "lucide-react";
import { useEffect, useRef } from "react";
import { useAuth } from "@/lib/auth";
import { cn } from "@/lib/utils";

const ITEMS = [
  { path: "/dashboard",    label: "Dashboard", icon: LayoutDashboard, mgr: false },
  { path: "/health",       label: "Health",    icon: Activity,        mgr: true  },
  { path: "/heatmap",      label: "Heatmap",   icon: CalendarDays,    mgr: true  },
  { path: "/inbox",        label: "Inbox",     icon: Inbox,           mgr: false },
  { path: "/tasks",        label: "Tasks",     icon: ListChecks,      mgr: false },
  { path: "/daily",        label: "Daily",     icon: PenLine,         mgr: false },
  { path: "/feed",         label: "Feed",      icon: Rss,             mgr: false },
  { path: "/achievements", label: "Awards",    icon: Trophy,          mgr: false },
  { path: "/events",       label: "Events",    icon: PartyPopper,     mgr: false },
  { path: "/leaderboard",  label: "Ranks",     icon: BarChart3,       mgr: false },
  { path: "/validation",   label: "Approve",   icon: CheckCircle2,    mgr: true  },
  { path: "/reports",      label: "Reports",   icon: FileText,        mgr: false },
  { path: "/profile",      label: "Profile",   icon: User,            mgr: false },
] as const;

export function BottomNav() {
  const { isManager } = useAuth();
  const { pathname } = useLocation();
  const navRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const active = navRef.current?.querySelector<HTMLAnchorElement>("[aria-current='page']");
    active?.scrollIntoView({ block: "nearest", inline: "center", behavior: "smooth" });
  }, [pathname]);

  const items = ITEMS.filter((i) => !i.mgr || isManager);

  return (
    <nav className="fixed bottom-0 left-0 right-0 z-40 border-t border-border/60 bg-background/85 backdrop-blur-xl shadow-[0_-4px_24px_-8px_rgba(0,0,0,0.08)]">
      <div ref={navRef} className="mx-auto flex max-w-3xl gap-1 overflow-x-auto px-2 py-2 scrollbar-hide" style={{ paddingBottom: `max(8px, env(safe-area-inset-bottom))` }}>
        {items.map(({ path, label, icon: Icon }) => (
          <NavLink
            key={path}
            to={path}
            className={({ isActive }) =>
              cn(
                "inline-flex items-center gap-1.5 rounded-full px-3 py-2 text-xs font-semibold whitespace-nowrap transition-all shrink-0",
                isActive
                  ? "bg-grad-brand text-white shadow-glow"
                  : "text-muted-foreground hover:bg-primary/5 hover:text-primary"
              )
            }
          >
            <Icon className="h-4 w-4" />
            <span>{label}</span>
          </NavLink>
        ))}
      </div>
    </nav>
  );
}
