import { useState, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { Download, Lock, Edit, Sparkles, FileText } from "lucide-react";
import { api, getToken } from "@/lib/api";
import { cn, fmtDate } from "@/lib/utils";

interface ReportRow {
  reportRecordId: number;
  reportType: "Weekly" | "Monthly" | "Quarterly";
  status: "Draft" | "Finalized" | "Locked";
  startDate: string;
  endDate: string;
  createdAtUtc: string;
}

type TypeFilter = "All" | "Weekly" | "Monthly" | "Quarterly";

const TYPE_TONE: Record<ReportRow["reportType"], string> = {
  Weekly:    "bg-primary-container/30 text-primary",
  Monthly:   "bg-secondary-container/40 text-on-secondary-container",
  Quarterly: "bg-tertiary-container/40 text-on-tertiary-container",
};

export function Reports() {
  const [filter, setFilter] = useState<TypeFilter>("All");
  const { data, isLoading } = useQuery<ReportRow[]>({
    queryKey: ["reports"],
    queryFn: () => api<ReportRow[]>("/reports"),
  });

  const rows = useMemo(() => {
    if (!data) return [];
    return filter === "All" ? data : data.filter((r) => r.reportType === filter);
  }, [data, filter]);

  async function download(id: number) {
    const token = getToken();
    const res = await fetch(`/api/reports/${id}/export`, { headers: { Authorization: `Bearer ${token}` } });
    if (!res.ok) return;
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url; a.download = `report-${id}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Reports</h1>
        <p className="text-sm text-on-surface-variant mt-1">Manage and review your generated productivity audits.</p>
      </div>

      {/* Tab filter */}
      <div className="flex gap-2 overflow-x-auto scrollbar-hide pb-1">
        {(["All", "Weekly", "Monthly", "Quarterly"] as TypeFilter[]).map((t) => (
          <button
            key={t}
            onClick={() => setFilter(t)}
            className={cn(
              "rounded-full px-4 py-1.5 text-xs font-bold whitespace-nowrap transition border",
              filter === t
                ? "bg-primary text-on-primary border-primary shadow-glow"
                : "bg-surface-container border-outline-variant/30 text-on-surface-variant hover:bg-surface-container-high"
            )}
          >
            {t}
          </button>
        ))}
      </div>

      {/* List */}
      {isLoading ? (
        <div className="space-y-2">
          {[...Array(3)].map((_, i) => <div key={i} className="h-24 rounded-xl bg-surface-container animate-pulse" />)}
        </div>
      ) : rows.length === 0 ? (
        <div className="rounded-xl bg-surface-container border border-outline-variant/30 p-10 text-center">
          <FileText className="mx-auto h-10 w-10 text-on-surface-variant mb-3" />
          <p className="text-sm">No reports yet.</p>
        </div>
      ) : (
        <div className="space-y-3">
          {rows.map((r) => (
            <article key={r.reportRecordId} className="rounded-xl bg-surface-container border border-outline-variant/30 p-4 shadow-soft">
              <div className="flex items-start justify-between gap-3">
                <div className="flex-1 min-w-0">
                  <span className={cn("text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full", TYPE_TONE[r.reportType])}>
                    {r.reportType}
                  </span>
                  <h3 className="font-bold mt-2">
                    {r.reportType === "Weekly" && `Week of ${fmtDate(r.startDate)}`}
                    {r.reportType === "Monthly" && `${new Date(r.startDate).toLocaleString("en-IN", { month: "long", year: "numeric" })}`}
                    {r.reportType === "Quarterly" && `Q${Math.ceil((new Date(r.startDate).getMonth() + 1) / 3)} ${new Date(r.startDate).getFullYear()}`}
                  </h3>
                  <p className="text-xs text-on-surface-variant mt-1">
                    {fmtDate(r.startDate, "long")} — {fmtDate(r.endDate, "long")}
                  </p>
                </div>
                <div className="flex flex-col items-end gap-2">
                  <span className={cn(
                    "inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full",
                    r.status === "Locked"    && "bg-success-container/30 text-on-success-container",
                    r.status === "Draft"     && "bg-tertiary-container/30 text-on-tertiary-container",
                    r.status === "Finalized" && "bg-secondary-container/40 text-on-secondary-container",
                  )}>
                    {r.status === "Locked" ? <Lock className="h-3 w-3" /> : <Edit className="h-3 w-3" />}
                    {r.status}
                  </span>
                  <div className="flex items-center gap-1">
                    <button
                      onClick={() => download(r.reportRecordId)}
                      className="grid h-8 w-8 place-items-center rounded-full hover:bg-surface-container-high text-on-surface-variant hover:text-primary transition-colors"
                      aria-label="Download"
                    >
                      <Download className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}

      {/* AI Summary teaser */}
      <section className="relative rounded-xl bg-gradient-to-br from-primary/20 via-secondary/10 to-tertiary/5 border border-primary/20 p-5 overflow-hidden">
        <div className="absolute -right-4 -top-4 h-24 w-24 rounded-full bg-primary/20 blur-2xl" />
        <div className="relative">
          <div className="flex items-center gap-2 mb-2">
            <Sparkles className="h-4 w-4 text-primary" />
            <p className="text-[10px] font-bold uppercase tracking-wider text-primary">AI Summary</p>
          </div>
          <h3 className="font-bold">Real-time performance metrics tracking.</h3>
          <p className="text-xs text-on-surface-variant mt-1">Team velocity peaked during the August sprint. Bottlenecks were identified in the DevOps pipeline.</p>
          <button className="mt-3 inline-flex items-center gap-1.5 rounded-full bg-primary text-on-primary px-3 py-1.5 text-xs font-bold hover:opacity-95 transition shadow-glow">
            Open Deep Dive
          </button>
        </div>
      </section>
    </div>
  );
}
