import { useQuery } from "@tanstack/react-query";
import {
  Users, Ban, Clock, AlertTriangle, TrendingUp, Activity, AlertCircle,
} from "lucide-react";
import { api } from "@/lib/api";
import { cn, initials } from "@/lib/utils";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";

interface HealthData {
  totalTeam: number;
  submittedToday: number;
  participationPct: number;
  missingNames: string[];
  blockedTickets: Array<{ ticketNumber: string; description: string }>;
  pendingAchievements: number;
  pendingSales: number;
  openActionItems: number;
  overdueActionItems: number;
  billingBreakdown: Array<{ type: string; count: number }>;
  weeklyTrend: Array<{ date: string; count: number }>;
  engagementScore: number;
}

const BILLING_COLORS = ["hsl(var(--primary))", "hsl(var(--secondary))", "hsl(var(--tertiary))", "hsl(var(--success))", "hsl(var(--error))"];

export function Health() {
  const { data: h, isLoading } = useQuery<HealthData>({
    queryKey: ["team-health"],
    queryFn: () => api<HealthData>("/dashboard/team-health"),
  });

  if (isLoading || !h) {
    return <div className="space-y-3">{[...Array(4)].map((_, i) => <div key={i} className="h-32 rounded-xl bg-surface-container animate-pulse" />)}</div>;
  }

  const trendMax = Math.max(...h.weeklyTrend.map(t => t.count), 1);
  const totalBilling = h.billingBreakdown.reduce((s, b) => s + b.count, 0) || 1;

  return (
    <div className="space-y-5">
      {/* Hero */}
      <header>
        <h1 className="text-2xl font-bold tracking-tight">Team Health Overview</h1>
        <p className="text-sm text-on-surface-variant mt-1">Real-time performance and engagement metrics across your workspace.</p>
      </header>

      {/* 2x2 stat grid */}
      <div className="grid grid-cols-2 gap-3">
        <StatTile
          tone="success"
          icon={Users}
          value={`${h.participationPct}%`}
          label="Participation"
          delta={`${h.submittedToday}/${h.totalTeam} submitted`}
        />
        <StatTile
          tone={h.blockedTickets.length === 0 ? "success" : "error"}
          icon={Ban}
          value={String(h.blockedTickets.length)}
          label="Blocked"
          delta={h.blockedTickets.length === 0 ? "All clear" : "Active blockers"}
        />
        <StatTile
          tone={h.pendingAchievements + h.pendingSales === 0 ? "success" : "tertiary"}
          icon={Clock}
          value={String(h.pendingAchievements + h.pendingSales)}
          label="Pending"
          delta="Validations queued"
        />
        <StatTile
          tone={h.overdueActionItems === 0 ? "primary" : "error"}
          icon={AlertTriangle}
          value={String(h.openActionItems)}
          label="Action items"
          delta={h.overdueActionItems > 0 ? `${h.overdueActionItems} overdue` : "On track"}
        />
      </div>

      {/* Engagement score */}
      <section className="rounded-xl bg-surface-container border border-outline-variant/30 p-5 shadow-soft">
        <div className="flex items-center justify-between mb-2">
          <h2 className="text-lg font-bold">Engagement Score</h2>
          <span className="inline-flex items-center gap-1 text-xs font-bold text-success">
            <TrendingUp className="h-3.5 w-3.5" />
            {h.engagementScore >= 80 ? "Good" : h.engagementScore >= 60 ? "Steady" : "Needs attention"}
          </span>
        </div>
        <p className="text-xs text-on-surface-variant mb-3">Aggregate team sentiment and activity interaction.</p>
        <div className="flex items-baseline gap-1.5 mb-2">
          <span className="text-4xl font-bold tracking-tight bg-grad-primary bg-clip-text text-transparent">{h.engagementScore}</span>
          <span className="text-sm font-semibold text-on-surface-variant">/100</span>
        </div>
        <div className="h-2 rounded-full bg-surface-container-high overflow-hidden">
          <div className="h-full bg-grad-primary rounded-full transition-all" style={{ width: `${h.engagementScore}%` }} />
        </div>
        <div className="mt-2 flex justify-between text-[10px] text-on-surface-variant">
          <span>Low Engagement</span>
          <span>Peak Flow</span>
        </div>
      </section>

      {/* Participation history */}
      <section className="rounded-xl bg-surface-container border border-outline-variant/30 p-5 shadow-soft">
        <h2 className="text-lg font-bold mb-1">Participation History</h2>
        <p className="text-xs text-on-surface-variant mb-4">Weekly response frequency.</p>
        <div className="flex items-end gap-2 h-32">
          {h.weeklyTrend.map((t, i) => (
            <div key={i} className="flex-1 flex flex-col items-center gap-1.5">
              <div className="w-full flex-1 flex items-end">
                <div
                  className={cn(
                    "w-full rounded-md transition-all",
                    t.count > 0 ? "bg-grad-primary" : "bg-surface-container-high"
                  )}
                  style={{ height: `${(t.count / trendMax) * 100 || 4}%` }}
                />
              </div>
              <span className="text-[10px] font-semibold text-on-surface-variant">
                {new Date(t.date).toLocaleDateString("en-IN", { weekday: "short" })[0]}
              </span>
            </div>
          ))}
        </div>
      </section>

      {/* Billing breakdown */}
      {h.billingBreakdown.length > 0 && (
        <section className="rounded-xl bg-surface-container border border-outline-variant/30 p-5 shadow-soft">
          <h2 className="text-lg font-bold mb-4">Billing Breakdown</h2>
          <div className="flex items-center gap-5">
            <DonutChart segments={h.billingBreakdown.map((b, i) => ({
              value: b.count,
              color: BILLING_COLORS[i % BILLING_COLORS.length],
              label: b.type,
            }))} total={totalBilling} />
            <div className="flex-1 space-y-2">
              {h.billingBreakdown.map((b, i) => (
                <div key={b.type} className="flex items-center justify-between text-sm">
                  <div className="flex items-center gap-2 min-w-0">
                    <span className="h-2.5 w-2.5 rounded-sm shrink-0" style={{ background: BILLING_COLORS[i % BILLING_COLORS.length] }} />
                    <span className="truncate font-medium">{b.type}</span>
                  </div>
                  <span className="text-on-surface-variant font-semibold tabular-nums">{b.count}</span>
                </div>
              ))}
            </div>
          </div>
        </section>
      )}

      {/* Active blockers */}
      {h.blockedTickets.length > 0 && (
        <section>
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-bold">Active Blockers</h2>
            <button className="text-xs font-bold text-primary hover:opacity-80">View All</button>
          </div>
          <div className="space-y-2">
            {h.blockedTickets.slice(0, 4).map((t, i) => (
              <article key={i} className="rounded-xl bg-surface-container border-l-2 border-error border-y border-r border-outline-variant/30 p-3 flex items-start gap-3 shadow-soft">
                <div className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-error-container/30 text-error">
                  <AlertCircle className="h-4 w-4" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-xs font-bold text-error">{t.ticketNumber}</p>
                  <p className="text-sm font-semibold leading-snug truncate">{t.description}</p>
                </div>
              </article>
            ))}
          </div>
        </section>
      )}

      {/* Missing today */}
      {h.missingNames.length > 0 && (
        <section className="rounded-xl bg-surface-container border border-outline-variant/30 p-5 shadow-soft">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-bold">Missing Today</h2>
            <span className="text-xs text-on-surface-variant font-semibold">{h.missingNames.length} team members</span>
          </div>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            {h.missingNames.slice(0, 6).map((name) => (
              <div key={name} className="flex items-center gap-2 min-w-0">
                <Avatar className="h-8 w-8 text-xs shrink-0">
                  <AvatarFallback>{initials(name)}</AvatarFallback>
                </Avatar>
                <span className="text-sm font-medium truncate">{name}</span>
              </div>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}

interface Segment { value: number; color: string; label: string; }

function DonutChart({ segments, total }: { segments: Segment[]; total: number }) {
  const cx = 50, cy = 50, r = 40, c = 2 * Math.PI * r;
  let offset = 0;
  return (
    <div className="relative shrink-0">
      <svg viewBox="0 0 100 100" className="h-28 w-28 -rotate-90">
        <circle cx={cx} cy={cy} r={r} fill="none" stroke="hsl(var(--surface-container-high))" strokeWidth="12" />
        {segments.map((s, i) => {
          const len = (s.value / total) * c;
          const el = (
            <circle
              key={i}
              cx={cx} cy={cy} r={r}
              fill="none"
              stroke={s.color}
              strokeWidth="12"
              strokeDasharray={`${len} ${c - len}`}
              strokeDashoffset={-offset}
              strokeLinecap="butt"
            />
          );
          offset += len;
          return el;
        })}
      </svg>
      <div className="absolute inset-0 grid place-items-center text-center">
        <div>
          <p className="text-[9px] uppercase tracking-wider text-on-surface-variant font-bold">Total</p>
          <p className="text-lg font-bold tabular-nums">{total}</p>
        </div>
      </div>
    </div>
  );
}

interface TileProps { tone: "success" | "error" | "tertiary" | "primary"; icon: any; value: string; label: string; delta: string; }
function StatTile({ tone, icon: Icon, value, label, delta }: TileProps) {
  const palette = {
    success:  "bg-success-container/30  text-success",
    error:    "bg-error-container/40    text-error",
    tertiary: "bg-tertiary-container/30 text-tertiary",
    primary:  "bg-primary/10            text-primary",
  } as const;
  return (
    <article className="rounded-xl bg-surface-container border border-outline-variant/30 p-4 shadow-soft">
      <div className={cn("inline-grid h-8 w-8 place-items-center rounded-lg mb-2", palette[tone])}>
        <Icon className="h-4 w-4" />
      </div>
      <p className="text-2xl font-bold tracking-tight">{value}</p>
      <p className="text-[10px] font-bold uppercase tracking-wider text-on-surface-variant mt-0.5">{label}</p>
      <p className="text-[11px] text-on-surface-variant mt-2 truncate">{delta}</p>
    </article>
  );
}
