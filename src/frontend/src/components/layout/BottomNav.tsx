import { NavLink, useLocation, useNavigate } from "react-router-dom";
import {
  LayoutDashboard, Activity, CalendarDays, Inbox, ListChecks, PenLine,
  Rss, Trophy, PartyPopper, BarChart3, CheckCircle2, FileText, User,
  Grip, X,
} from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { useAuth } from "@/lib/auth";
import { cn } from "@/lib/utils";

interface NavItem {
  path: string;
  label: string;
  icon: any;
  mgr: boolean;
  // Primary = always in bottom dock; others live behind "More"
  primary?: boolean;
}

// SCR-1 / Assessment-1: 4 primary tabs everyone sees + "More" overflow sheet.
// Primary set chosen for daily flow (Home → Check-in → Feed → Profile).
// Role-specific items (Pulse, Calendar, Review) live inside More for managers only.
const ITEMS: NavItem[] = [
  { path: "/dashboard",    label: "Home",      icon: LayoutDashboard, mgr: false, primary: true  },
  { path: "/daily",        label: "Check-in",  icon: PenLine,         mgr: false, primary: true  },
  { path: "/feed",         label: "Feed",      icon: Rss,             mgr: false, primary: true  },
  { path: "/profile",      label: "Profile",   icon: User,            mgr: false, primary: true  },
  // Hidden under "More"
  { path: "/inbox",        label: "Inbox",     icon: Inbox,           mgr: false },
  { path: "/tasks",        label: "Tasks",     icon: ListChecks,      mgr: false },
  { path: "/achievements", label: "Kudos",     icon: Trophy,          mgr: false },
  { path: "/events",       label: "Events",    icon: PartyPopper,     mgr: false },
  { path: "/leaderboard",  label: "Top 10",    icon: BarChart3,       mgr: false },
  { path: "/reports",      label: "Reports",   icon: FileText,        mgr: false },
  // Manager-only items
  { path: "/health",       label: "Pulse",     icon: Activity,        mgr: true  },
  { path: "/heatmap",      label: "Calendar",  icon: CalendarDays,    mgr: true  },
  { path: "/validation",   label: "Review",    icon: CheckCircle2,    mgr: true  },
];

export function BottomNav() {
  const { isManager } = useAuth();
  const { pathname } = useLocation();
  const nav = useNavigate();
  const [moreOpen, setMoreOpen] = useState(false);
  const sheetRef = useRef<HTMLDivElement>(null);

  // Filter by role
  const visibleItems  = ITEMS.filter((i) => !i.mgr || isManager);
  const primaryItems  = visibleItems.filter((i) => i.primary);
  const overflowItems = visibleItems.filter((i) => !i.primary);

  // If user is on an overflow page, mark the More button active
  const isOverflowActive = overflowItems.some((i) => i.path === pathname);

  // Close sheet on route change
  useEffect(() => { setMoreOpen(false); }, [pathname]);

  // ESC + outside click dismiss
  useEffect(() => {
    if (!moreOpen) return;
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") setMoreOpen(false);
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [moreOpen]);

  return (
    <>
      {/* ───────── Bottom dock ───────── */}
      <nav className="fixed bottom-0 left-0 right-0 z-50 rounded-t-xl bg-surface-container-low/90 backdrop-blur-xl border-t border-outline-variant/30 shadow-lg">
        <div
          className="mx-auto flex max-w-3xl items-stretch gap-1 px-2 py-2"
          style={{ paddingBottom: `max(8px, env(safe-area-inset-bottom))` }}
        >
          {primaryItems.map(({ path, label, icon: Icon }) => (
            <NavLink
              key={path}
              to={path}
              className={({ isActive }) =>
                cn(
                  "group flex-1 inline-flex flex-col items-center gap-0.5 rounded-xl px-2 py-1.5 text-[10px] font-semibold whitespace-nowrap transition-all min-w-0",
                  isActive ? "text-primary" : "text-on-surface-variant hover:text-primary"
                )
              }
            >
              {({ isActive }) => (
                <>
                  <span className={cn(
                    "grid h-7 w-12 place-items-center rounded-full transition-colors",
                    isActive ? "bg-primary/15" : "group-hover:bg-primary/10"
                  )}>
                    <Icon className="h-4 w-4" strokeWidth={isActive ? 2.4 : 2} />
                  </span>
                  <span className={cn("tracking-tight", isActive && "font-bold")}>{label}</span>
                </>
              )}
            </NavLink>
          ))}

          {/* More button — opens overflow sheet */}
          {overflowItems.length > 0 && (
            <button
              type="button"
              onClick={() => setMoreOpen((o) => !o)}
              aria-expanded={moreOpen}
              aria-controls="more-sheet"
              className={cn(
                "group flex-1 inline-flex flex-col items-center gap-0.5 rounded-xl px-2 py-1.5 text-[10px] font-semibold whitespace-nowrap transition-all min-w-0 bg-transparent shadow-none",
                (moreOpen || isOverflowActive) ? "text-primary" : "text-on-surface-variant hover:text-primary"
              )}
            >
              <span className={cn(
                "grid h-7 w-12 place-items-center rounded-full transition-colors",
                (moreOpen || isOverflowActive) ? "bg-primary/15" : "group-hover:bg-primary/10"
              )}>
                {moreOpen ? <X className="h-4 w-4" strokeWidth={2.4} /> : <Grip className="h-4 w-4" strokeWidth={isOverflowActive ? 2.4 : 2} />}
              </span>
              <span className={cn("tracking-tight", (moreOpen || isOverflowActive) && "font-bold")}>
                {moreOpen ? "Close" : "More"}
              </span>
            </button>
          )}
        </div>
      </nav>

      {/* ───────── More sheet (backdrop + slide-up panel) ───────── */}
      {moreOpen && (
        <>
          {/* Backdrop */}
          <div
            className="fixed inset-0 z-40 bg-background/60 backdrop-blur-sm animate-fade-in"
            onClick={() => setMoreOpen(false)}
            aria-hidden
          />
          {/* Sheet */}
          <div
            id="more-sheet"
            ref={sheetRef}
            role="dialog"
            aria-label="More"
            className="fixed bottom-[72px] left-0 right-0 z-50 mx-auto max-w-3xl px-3 animate-slide-up"
            style={{ paddingBottom: `max(0px, env(safe-area-inset-bottom))` }}
          >
            <div className="rounded-2xl bg-surface-container border border-outline-variant/40 shadow-glow p-3">
              <div className="flex items-center justify-between px-2 pb-2">
                <p className="text-[11px] font-bold uppercase tracking-wider text-on-surface-variant">
                  More
                </p>
                <button
                  onClick={() => setMoreOpen(false)}
                  className="grid h-7 w-7 place-items-center rounded-full text-on-surface-variant hover:bg-surface-container-high transition-colors bg-transparent shadow-none"
                  aria-label="Close"
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              </div>
              <div className="grid grid-cols-3 sm:grid-cols-4 gap-2">
                {overflowItems.map(({ path, label, icon: Icon, mgr }) => {
                  const active = pathname === path;
                  return (
                    <button
                      key={path}
                      type="button"
                      onClick={() => { nav(path); setMoreOpen(false); }}
                      className={cn(
                        "flex flex-col items-center gap-2 rounded-xl p-3 transition-colors border bg-transparent shadow-none",
                        active
                          ? "bg-primary/10 border-primary/40 text-primary"
                          : "border-outline-variant/30 hover:bg-surface-container-high text-on-surface"
                      )}
                    >
                      <span className={cn(
                        "grid h-10 w-10 place-items-center rounded-xl",
                        active
                          ? "bg-grad-primary text-on-primary shadow-glow"
                          : "bg-surface-container-high text-on-surface-variant"
                      )}>
                        <Icon className="h-5 w-5" />
                      </span>
                      <span className={cn("text-xs font-semibold", active && "font-bold")}>{label}</span>
                      {mgr && (
                        <span className="text-[8px] font-bold uppercase tracking-wider text-tertiary -mt-1">
                          Manager
                        </span>
                      )}
                    </button>
                  );
                })}
              </div>
            </div>
          </div>
        </>
      )}
    </>
  );
}
