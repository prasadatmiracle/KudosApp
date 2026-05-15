import { useState, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { Calendar, Users, ChevronDown } from "lucide-react";
import { api } from "@/lib/api";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { cn, initials } from "@/lib/utils";

type Range = "30" | "60" | "90";
const RANGE_LABEL: Record<Range, string> = { "30": "Last 30 Days", "60": "Last 60 Days", "90": "Last 90 Days" };

interface HeatmapDay { date: string; submitted: boolean; status?: string; isWeekend?: boolean }
interface HeatmapRow { userId: number; name: string; days: HeatmapDay[]; }
// Backend wraps rows: { dates: string[], users: HeatmapRow[] }
interface HeatmapResponse { dates: string[]; users: HeatmapRow[]; }

export function Heatmap() {
  const [range, setRange] = useState<Range>("90");

  const today = new Date();
  const endDate = today.toISOString().slice(0, 10);
  const startDate = new Date(today.getTime() - parseInt(range) * 86_400_000).toISOString().slice(0, 10);

  const { data: payload, isLoading, error } = useQuery<HeatmapResponse>({
    queryKey: ["heatmap-range", startDate, endDate],
    queryFn: () => api<HeatmapResponse>(`/daily-updates/compliance-heatmap/range?startDate=${startDate}&endDate=${endDate}`),
  });
  const data = payload?.users;

  const ROLE_HUE: Record<string, string> = useMemo(() => ({
    "Lead Developer": "secondary",
    "Designer":       "primary",
    "Product":        "tertiary",
    "Manager":        "primary",
    "Admin":          "tertiary",
    "Employee":       "secondary",
  }), []);

  return (
    <div className="space-y-5">
      {/* Filter row */}
      <div className="flex flex-wrap items-center gap-2">
        <FilterPill icon={Calendar} value={RANGE_LABEL[range]} onSelect={() => {
          const order: Range[] = ["30", "60", "90"];
          const next = order[(order.indexOf(range) + 1) % order.length];
          setRange(next);
        }} />
        <FilterPill icon={Users} value="All Team Members" />
        {/* Avatar stack */}
        {data && data.length > 0 && (
          <div className="ml-auto flex items-center -space-x-2">
            {data.slice(0, 3).map((r) => (
              <Avatar key={r.userId} className="h-7 w-7 ring-2 ring-background text-[10px]">
                <AvatarFallback>{initials(r.name)}</AvatarFallback>
              </Avatar>
            ))}
            {data.length > 3 && (
              <div className="h-7 w-7 rounded-full bg-surface-container-high border-2 border-background grid place-items-center text-[10px] font-bold text-on-surface-variant">
                +{data.length - 3}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Per-person heatmaps */}
      {isLoading ? (
        <div className="space-y-3">
          {[...Array(3)].map((_, i) => <div key={i} className="h-44 rounded-xl bg-surface-container animate-pulse" />)}
        </div>
      ) : error ? (
        <div className="rounded-xl border border-error/30 bg-error-container/30 p-6 text-sm text-on-error-container">
          <p className="font-bold mb-1">Couldn't load the participation calendar.</p>
          <p className="text-xs opacity-90">{(error as Error)?.message ?? "Unknown error"}</p>
        </div>
      ) : !data || data.length === 0 ? (
        <div className="rounded-xl bg-surface-container border border-outline-variant/30 p-10 text-center">
          <Calendar className="mx-auto h-10 w-10 text-on-surface-variant mb-3" />
          <p className="text-sm">No participation data yet for this range.</p>
          <p className="text-xs text-on-surface-variant mt-1">Once your team submits check-ins they'll appear here.</p>
        </div>
      ) : (
        <div className="space-y-4">
          {data.map((row) => {
            const submittedCount = row.days.filter(d => d.submitted).length;
            const role = "Developer"; // placeholder until backend returns role
            const hue = ROLE_HUE[role] ?? "secondary";
            return (
              <article key={row.userId} className="rounded-xl bg-surface-container border border-outline-variant/30 p-4 shadow-soft">
                <header className="flex items-center justify-between mb-3 flex-wrap gap-2">
                  <div className="flex items-center gap-2 min-w-0">
                    <h3 className="text-base font-bold truncate">{row.name}</h3>
                    <span className={cn(
                      "text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full",
                      hue === "primary"   && "bg-primary/15 text-primary",
                      hue === "secondary" && "bg-secondary-container/40 text-on-secondary-container",
                      hue === "tertiary"  && "bg-tertiary-container/30 text-on-tertiary-container",
                    )}>
                      {role}
                    </span>
                  </div>
                  <span className="text-xs text-on-surface-variant font-semibold">
                    {submittedCount} interactions
                  </span>
                </header>
                <CellGrid days={row.days} role={hue} />
              </article>
            );
          })}
        </div>
      )}

      {/* Legend */}
      <div className="flex items-center gap-4 flex-wrap text-[10px] text-on-surface-variant pt-2">
        <div className="flex items-center gap-2">
          <span className="font-bold uppercase tracking-wider">Less</span>
          {[0.2, 0.4, 0.6, 0.8, 1.0].map((opacity, i) => (
            <span key={i} className="h-3.5 w-3.5 rounded-sm" style={{ background: `hsl(var(--primary) / ${opacity})` }} />
          ))}
          <span className="font-bold uppercase tracking-wider">More</span>
        </div>
        <div className="flex items-center gap-1.5">
          <span className="h-3.5 w-3.5 rounded-sm" style={{ background: "hsl(var(--outline-variant) / 0.6)" }} />
          <span>Focus day</span>
        </div>
      </div>
    </div>
  );
}

function CellGrid({ days, role }: { days: HeatmapDay[]; role: string }) {
  // Render as a grid of 7 rows (days of week)
  const cells = days.slice(-70); // last 10 weeks
  return (
    <div className="grid grid-flow-col grid-rows-7 gap-1">
      {cells.map((d, i) => {
        const colorVar = role === "primary" ? "--primary"
                       : role === "tertiary" ? "--tertiary"
                       : "--secondary";
        // SCR-1 C6 / Assessment A1: FocusDay + Continuing render in a neutral
        // tone — not punitive red, not bright "completed" green.
        const isFocus = d.status === "FocusDay" || d.status === "Continuing" || d.status === "NoTask";
        let background: string;
        if (isFocus)            background = "hsl(var(--outline-variant) / 0.6)";
        else if (d.submitted)   background = `hsl(var(${colorVar}) / ${d.status === "Blocked" ? 0.55 : 0.9})`;
        else                    background = "hsl(var(--surface-container-high))";
        const title = `${d.date}${d.submitted ? ` · ${d.status ?? "submitted"}` : " · no entry"}`;
        return (
          <div
            key={i}
            title={title}
            className="h-3.5 w-3.5 rounded-sm transition-transform hover:scale-150 hover:z-10 relative"
            style={{ background }}
          />
        );
      })}
    </div>
  );
}

function FilterPill({ icon: Icon, value, onSelect }: { icon: any; value: string; onSelect?: () => void }) {
  return (
    <button
      onClick={onSelect}
      className="inline-flex items-center gap-2 rounded-full border border-outline-variant bg-surface-container px-3.5 py-1.5 text-xs font-bold hover:bg-surface-container-high transition"
    >
      <Icon className="h-3.5 w-3.5 text-primary" />
      {value}
      <ChevronDown className="h-3.5 w-3.5 opacity-60" />
    </button>
  );
}
