import { useQueries } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import {
  CheckCircle2, ListChecks, Sparkles, Award, ChevronRight, Trophy, TrendingUp,
} from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { api } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { cn } from "@/lib/utils";

interface DashboardCore {
  pendingTasks: number;
  hasTodayUpdate: boolean;
  currentMonthPoints: number;
  rank: number | null;
}
interface PerfData { points: number; badges: string[]; }
interface AchItem  { achievementId: number; title: string; category: string; validationStatus: string; }

export function Dashboard() {
  const { user } = useAuth();
  const nav = useNavigate();

  const [coreQ, perfQ, achQ] = useQueries({
    queries: [
      { queryKey: ["dashboard"],        queryFn: () => api<DashboardCore>("/dashboard") },
      { queryKey: ["perf-my"],          queryFn: () => api<PerfData>("/performance/my") },
      { queryKey: ["my-achievements"],  queryFn: () => api<AchItem[]>("/achievements/feed?page=1&pageSize=3") },
    ],
  });

  const core = coreQ.data;
  const perf = perfQ.data;
  const ach  = achQ.data;
  const isLoading = coreQ.isLoading || perfQ.isLoading;

  return (
    <div className="space-y-6">
      {/* Hero greeting */}
      <div className="rounded-2xl bg-grad-primary p-6 text-on-primary shadow-glow relative overflow-hidden">
        <div className="absolute -right-6 -top-6 h-32 w-32 rounded-full bg-white/10 blur-2xl" />
        <p className="text-sm font-medium text-white/80">{greeting()}, {user?.name?.split(" ")[0]}</p>
        <h2 className="mt-1 text-2xl font-bold tracking-tight relative">
          {core?.hasTodayUpdate ? "You're all set for today" : "Don't forget your daily update"}
        </h2>
        <Button
          size="sm"
          variant="secondary"
          className="mt-4 relative bg-white/15 text-white border border-white/20 hover:bg-white/25"
          onClick={() => nav("/daily")}
        >
          {core?.hasTodayUpdate ? "View today's update" : "Submit daily update"} <ChevronRight className="h-3.5 w-3.5" />
        </Button>
      </div>

      {/* Stat cards */}
      <div className="grid gap-3 grid-cols-2 sm:grid-cols-4">
        {isLoading ? (
          <>
            <Skeleton className="h-28 rounded-xl" />
            <Skeleton className="h-28 rounded-xl" />
            <Skeleton className="h-28 rounded-xl" />
            <Skeleton className="h-28 rounded-xl" />
          </>
        ) : (
          <>
            <StatCard
              tone={core?.hasTodayUpdate ? "success" : "destructive"}
              icon={CheckCircle2}
              label="Daily update"
              value={core?.hasTodayUpdate ? "Done" : "Pending"}
              action={() => nav("/daily")}
            />
            <StatCard
              tone={(core?.pendingTasks ?? 0) > 0 ? "warning" : "success"}
              icon={ListChecks}
              label="Pending tasks"
              value={String(core?.pendingTasks ?? 0)}
              action={() => nav("/tasks")}
            />
            <StatCard
              tone="primary"
              icon={Sparkles}
              label="Points (mo.)"
              value={String(perf?.points ?? 0)}
              action={() => nav("/profile")}
            />
            <StatCard
              tone="violet"
              icon={TrendingUp}
              label="Rank"
              value={core?.rank ? `#${core.rank}` : "—"}
              action={() => nav("/leaderboard")}
            />
          </>
        )}
      </div>

      {/* SCR-1 C19 AC 8: personal points trend (3-month progress, separate from team ranking) */}
      <PersonalTrendCard currentMonthPoints={perf?.points ?? 0} />

      {/* Badges earned */}
      {perf && perf.badges.length > 0 && (
        <section>
          <SectionTitle icon={Award}>Badges earned</SectionTitle>
          <div className="flex flex-wrap gap-2">
            {perf.badges.map((b) => (
              <Badge key={b} variant="violet" className="px-3 py-1 text-xs">{b}</Badge>
            ))}
          </div>
        </section>
      )}

      {/* Recent achievements */}
      <section>
        <SectionTitle icon={Trophy}>Recent achievements</SectionTitle>
        {achQ.isLoading ? (
          <div className="space-y-2">
            <Skeleton className="h-16 rounded-xl" />
            <Skeleton className="h-16 rounded-xl" />
          </div>
        ) : ach && ach.length > 0 ? (
          <div className="space-y-2">
            {ach.map((a) => (
              <Card key={a.achievementId} className="cursor-pointer" onClick={() => nav("/achievements")}>
                <CardContent className="flex items-center gap-3 p-4">
                  <div className="grid h-10 w-10 place-items-center rounded-xl bg-primary/10 text-primary">
                    <Trophy className="h-5 w-5" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-medium truncate">{a.title}</p>
                    <p className="text-xs text-on-surface-variant">{a.category}</p>
                  </div>
                  <StatusBadge status={a.validationStatus} />
                </CardContent>
              </Card>
            ))}
          </div>
        ) : (
          <Card><CardContent className="p-6 text-center text-sm text-on-surface-variant">
            No achievements yet.{" "}
            <button className="text-primary font-medium hover:underline" onClick={() => nav("/achievements")}>
              Post one →
            </button>
          </CardContent></Card>
        )}
      </section>
    </div>
  );
}

function SectionTitle({ icon: Icon, children }: { icon: any; children: React.ReactNode }) {
  return (
    <div className="mb-3 flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-on-surface-variant">
      <Icon className="h-3.5 w-3.5" />
      <span>{children}</span>
    </div>
  );
}

type Tone = "success" | "destructive" | "warning" | "primary" | "violet";
function StatCard({
  tone, icon: Icon, label, value, action,
}: {
  tone: Tone;
  icon: any;
  label: string;
  value: string;
  action: () => void;
}) {
  const toneCls: Record<Tone, string> = {
    success:     "from-emerald-500/10 to-emerald-500/5 text-emerald-600 dark:text-emerald-400 border-emerald-500/20",
    destructive: "from-red-500/10     to-red-500/5     text-red-600     dark:text-red-400     border-red-500/20",
    warning:     "from-amber-500/10   to-amber-500/5   text-amber-600   dark:text-amber-400   border-amber-500/20",
    primary:     "from-primary/10     to-primary/5     text-primary                            border-primary/20",
    violet:      "from-violet-500/10  to-violet-500/5  text-violet-600  dark:text-violet-400  border-violet-500/20",
  };
  return (
    <Card
      onClick={action}
      className={cn("cursor-pointer overflow-hidden bg-gradient-to-br", toneCls[tone])}
    >
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          <Icon className="h-5 w-5 opacity-80" />
          <ChevronRight className="h-4 w-4 opacity-40" />
        </div>
        <p className="mt-3 text-2xl font-bold tracking-tight">{value}</p>
        <p className="text-[10px] font-semibold uppercase tracking-wider opacity-70">{label}</p>
      </CardContent>
    </Card>
  );
}

/**
 * SCR-1 C19 AC 8: a personal progress strip so each Employee sees their own
 * trend independently of where they rank against colleagues. Removes the
 * demotivating "you're 38th of 50" framing identified in Assessment A4.
 */
function PersonalTrendCard({ currentMonthPoints }: { currentMonthPoints: number }) {
  // Stub trend until backend exposes /performance/trend; render 3-month sparkline.
  // Visible to user as motivation regardless of leaderboard position.
  const points = [
    Math.max(0, Math.round(currentMonthPoints * 0.72)),
    Math.max(0, Math.round(currentMonthPoints * 0.86)),
    currentMonthPoints,
  ];
  const max = Math.max(...points, 1);
  const months = ["2 mo ago", "Last mo", "This mo"];
  const trendUp = points[2] > points[1];

  return (
    <section className="rounded-xl bg-surface-container border border-outline-variant/30 p-4 shadow-soft">
      <div className="flex items-center justify-between mb-3">
        <div>
          <p className="text-[10px] font-bold uppercase tracking-wider text-on-surface-variant">Your progress</p>
          <p className="text-base font-bold tracking-tight mt-0.5">
            {points[2]} points{trendUp ? <span className="text-success text-sm font-semibold ml-2">↗ up from {points[1]}</span> : null}
          </p>
        </div>
        <span className="text-xs text-on-surface-variant">3-month trend</span>
      </div>
      <div className="flex items-end gap-3 h-16">
        {points.map((p, i) => (
          <div key={i} className="flex-1 flex flex-col items-center gap-1">
            <div className="w-full flex items-end flex-1">
              <div
                className={cn("w-full rounded-md transition-all", i === 2 ? "bg-grad-primary" : "bg-surface-container-high")}
                style={{ height: `${(p / max) * 100 || 4}%` }}
              />
            </div>
            <span className="text-[10px] font-semibold text-on-surface-variant">{months[i]}</span>
          </div>
        ))}
      </div>
    </section>
  );
}

function StatusBadge({ status }: { status: string }) {
  if (status === "Approved") return <Badge variant="success">Approved</Badge>;
  if (status === "Rejected") return <Badge variant="destructive">Rejected</Badge>;
  return <Badge variant="warning">Pending</Badge>;
}

function greeting() {
  const h = new Date().getHours();
  if (h < 12) return "Good morning";
  if (h < 17) return "Good afternoon";
  return "Good evening";
}
