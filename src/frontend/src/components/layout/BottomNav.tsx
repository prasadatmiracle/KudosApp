import { NavLink, useLocation } from "react-router-dom";
import {
  LayoutDashboard, Activity, CalendarDays, Inbox, ListChecks, PenLine,
  Rss, Trophy, PartyPopper, BarChart3, CheckCircle2, FileText, User,
} from "lucide-react";
import { useEffect, useRef } from "react";
import { useAuth } from "@/lib/auth";
import { cn } from "@/lib/utils";

// SCR-1 / Assessment-1: nav labels reframed for engagement, not surveillance.
const ITEMS = [
  { path: "/dashboard",    label: "Home",      icon: LayoutDashboard, mgr: false },
  { path: "/health",       label: "Pulse",     icon: Activity,        mgr: true  },
  { path: "/heatmap",      label: "Calendar",  icon: CalendarDays,    mgr: true  },   // was "Heatmap" → C1/C2
  { path: "/inbox",        label: "Inbox",     icon: Inbox,           mgr: false },
  { path: "/tasks",        label: "Tasks",     icon: ListChecks,      mgr: false },
  { path: "/daily",        label: "Check-in",  icon: PenLine,         mgr: false },   // was "Daily" → friendlier
  { path: "/feed",         label: "Feed",      icon: Rss,             mgr: false },
  { path: "/achievements", label: "Kudos",     icon: Trophy,          mgr: false },   // was "Awards"
  { path: "/events",       label: "Events",    icon: PartyPopper,     mgr: false },
  { path: "/leaderboard",  label: "Top 10",    icon: BarChart3,       mgr: false },   // implies aspirational, not ranking
  { path: "/validation",   label: "Review",    icon: CheckCircle2,    mgr: true  },   // was "Queue"
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
    <nav className="fixed bottom-0 left-0 right-0 z-50 rounded-t-xl bg-surface-container-low/90 backdrop-blur-xl border-t border-outline-variant/30 shadow-lg">
      <div
        ref={navRef}
        className="mx-auto flex max-w-3xl gap-1 overflow-x-auto px-2 py-2 scrollbar-hide"
        style={{ paddingBottom: `max(8px, env(safe-area-inset-bottom))` }}
      >
        {items.map(({ path, label, icon: Icon }) => (
          <NavLink
            key={path}
            to={path}
            className={({ isActive }) =>
              cn(
                "group inline-flex flex-col items-center gap-0.5 rounded-xl px-3 py-1.5 text-[10px] font-semibold whitespace-nowrap transition-all shrink-0 min-w-[58px]",
                isActive
                  ? "text-primary"
                  : "text-on-surface-variant hover:text-primary"
              )
            }
          >
            {({ isActive }) => (
              <>
                <span className={cn(
                  "grid h-7 w-12 place-items-center rounded-full transition-colors",
                  isActive
                    ? "bg-primary/15"
                    : "group-hover:bg-primary/10"
                )}>
                  <Icon className="h-4 w-4" strokeWidth={isActive ? 2.4 : 2} />
                </span>
                <span className={cn("tracking-tight", isActive && "font-bold")}>{label}</span>
              </>
            )}
          </NavLink>
        ))}
      </div>
    </nav>
  );
}
