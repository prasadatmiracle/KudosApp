import { useState, type FormEvent, useEffect } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  CloudOff, X, Send, Flame, ChevronDown, CheckCircle2, AlertCircle, Loader2,
} from "lucide-react";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { api } from "@/lib/api";
import { cn, fmtDate } from "@/lib/utils";
import { toast } from "sonner";

interface DailyUpdate {
  dailyUpdateId: number;
  workDate: string;
  ticketNo: string;
  description: string;
  status: string;
}

const STATUSES = ["Open", "InProgress", "Completed", "Blocked", "NoTask"] as const;

const STATUS_TONE: Record<string, string> = {
  Completed:  "bg-success-container/40 text-on-success-container",
  InProgress: "bg-tertiary-container/30 text-on-tertiary-container",
  Blocked:    "bg-error-container/40   text-on-error-container",
  Open:       "bg-surface-container-high text-on-surface-variant",
  NoTask:     "bg-surface-container-high text-on-surface-variant",
};

export function Daily() {
  const qc = useQueryClient();
  const [workDate, setWorkDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [ticketNo, setTicket] = useState("");
  const [description, setDesc] = useState("");
  const [status, setStatus] = useState<typeof STATUSES[number]>("InProgress");
  const [offline, setOffline] = useState(!navigator.onLine);
  const [showOfflineBanner, setShowOfflineBanner] = useState(true);

  useEffect(() => {
    const on  = () => { setOffline(false); setShowOfflineBanner(true); };
    const off = () => { setOffline(true);  setShowOfflineBanner(true); };
    window.addEventListener("online", on);
    window.addEventListener("offline", off);
    return () => {
      window.removeEventListener("online", on);
      window.removeEventListener("offline", off);
    };
  }, []);

  const { data: mine, isLoading } = useQuery<DailyUpdate[]>({
    queryKey: ["daily-mine"],
    queryFn: () => api<DailyUpdate[]>("/daily-updates/my?days=7"),
  });

  const submit = useMutation({
    mutationFn: (body: { ticketNo: string; description: string; status: string }) =>
      api("/daily-updates", { method: "POST", body }),
    onSuccess: () => {
      toast.success("Daily update submitted");
      setTicket(""); setDesc("");
      localStorage.removeItem("kudos.daily.draft");
      qc.invalidateQueries({ queryKey: ["daily-mine"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
    },
    onError: (e: any) => toast.error(e?.message ?? "Could not save"),
  });

  // Draft restore + save
  useEffect(() => {
    const draft = localStorage.getItem("kudos.daily.draft");
    if (draft) {
      try {
        const d = JSON.parse(draft);
        setTicket(d.ticketNo ?? ""); setDesc(d.description ?? "");
        if (d.status) setStatus(d.status);
      } catch { /* ignore */ }
    }
  }, []);
  useEffect(() => {
    if (ticketNo || description) {
      localStorage.setItem("kudos.daily.draft", JSON.stringify({ ticketNo, description, status }));
    }
  }, [ticketNo, description, status]);

  function onSubmit(e: FormEvent) {
    e.preventDefault();
    submit.mutate({ ticketNo, description, status });
  }

  return (
    <div className="space-y-6">
      {/* Offline banner */}
      {offline && showOfflineBanner && (
        <div className="flex items-center gap-3 rounded-xl border border-tertiary/30 bg-tertiary-container/30 px-4 py-3 text-on-tertiary-container animate-slide-up">
          <CloudOff className="h-5 w-5 shrink-0" />
          <div className="flex-1 min-w-0">
            <p className="text-sm font-bold">Offline Draft Mode</p>
            <p className="text-xs opacity-90">Your changes are saved locally and will sync once reconnected.</p>
          </div>
          <button
            onClick={() => setShowOfflineBanner(false)}
            className="p-1 hover:bg-on-tertiary-container/10 rounded-full transition-colors"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      )}

      {/* Form card */}
      <section className="rounded-xl bg-surface-container border border-outline-variant/30 p-6 shadow-soft space-y-5">
        <header>
          <h2 className="text-xl font-bold tracking-tight">Daily Update</h2>
          <p className="text-sm text-on-surface-variant mt-1">Submit your work log for today's focus session.</p>
        </header>

        <form onSubmit={onSubmit} className="space-y-5">
          {/* Work Date */}
          <div className="space-y-2">
            <label className="text-xs font-semibold uppercase tracking-wider text-on-surface-variant">Work Date</label>
            <Input
              type="date"
              value={workDate}
              onChange={(e) => setWorkDate(e.target.value)}
              className="bg-background border-outline-variant"
            />
          </div>

          {/* Project / Ticket */}
          <div className="space-y-2">
            <label className="text-xs font-semibold uppercase tracking-wider text-on-surface-variant">Project</label>
            <div className="relative">
              <select className="w-full h-10 rounded-md border border-outline-variant bg-background px-3 pr-9 text-sm appearance-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary">
                <option>Apollo Design System</option>
                <option>Atlas Sales Engine</option>
                <option>Orion Platform</option>
              </select>
              <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 text-on-surface-variant pointer-events-none" />
            </div>
          </div>

          {/* Status pills */}
          <div className="space-y-2">
            <label className="text-xs font-semibold uppercase tracking-wider text-on-surface-variant">Current Status</label>
            <div className="flex flex-wrap gap-2">
              {STATUSES.map((s) => (
                <button
                  key={s}
                  type="button"
                  onClick={() => setStatus(s)}
                  className={cn(
                    "rounded-full border px-4 py-1.5 text-xs font-bold transition-all",
                    status === s
                      ? "border-primary bg-primary/10 text-primary"
                      : "border-outline-variant text-on-surface-variant hover:bg-surface-container-high"
                  )}
                >
                  {s}
                </button>
              ))}
            </div>
          </div>

          {/* Ticket # */}
          <div className="space-y-2">
            <label className="text-xs font-semibold uppercase tracking-wider text-on-surface-variant">Ticket #</label>
            <Input
              value={ticketNo}
              onChange={(e) => setTicket(e.target.value)}
              placeholder="e.g. ENG-1234"
              className="bg-background border-outline-variant"
              required
            />
          </div>

          {/* Description */}
          <div className="space-y-2">
            <label className="text-xs font-semibold uppercase tracking-wider text-on-surface-variant">Work Description</label>
            <Textarea
              value={description}
              onChange={(e) => setDesc(e.target.value)}
              placeholder="What did you achieve today?"
              rows={3}
              className="bg-background border-outline-variant"
              required
            />
          </div>

          {/* Submit */}
          <button
            type="submit"
            disabled={submit.isPending}
            className="w-full h-12 rounded-xl bg-grad-primary text-on-primary font-bold text-sm shadow-glow hover:opacity-95 transition active:scale-[0.99] inline-flex items-center justify-center gap-2 disabled:opacity-60"
          >
            {submit.isPending
              ? <><Loader2 className="h-4 w-4 animate-spin" /> Saving…</>
              : <>Submit Update <Send className="h-4 w-4" /></>}
          </button>
        </form>
      </section>

      {/* Streak card */}
      <section className="rounded-xl bg-surface-container border border-outline-variant/30 p-4 flex items-center gap-4 shadow-soft">
        <div className="grid h-12 w-12 place-items-center rounded-full bg-tertiary-container/40">
          <Flame className="h-6 w-6 text-tertiary" />
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-bold">
            {(mine?.length ?? 0)} Day Streak
          </p>
          <p className="text-xs text-on-surface-variant">
            You're in the top 5% of contributors this month!
          </p>
        </div>
        <span className="text-[10px] font-bold uppercase tracking-wider text-success bg-success-container/30 px-2 py-1 rounded-full">
          LIVE
        </span>
      </section>

      {/* Recent submissions */}
      <section>
        <h3 className="mb-3 text-xs font-bold uppercase tracking-wider text-on-surface-variant">My last 7 days</h3>
        {isLoading ? (
          <div className="space-y-2">
            <div className="h-20 rounded-xl bg-surface-container animate-pulse" />
            <div className="h-20 rounded-xl bg-surface-container animate-pulse" />
          </div>
        ) : !mine || mine.length === 0 ? (
          <div className="rounded-xl bg-surface-container border border-outline-variant/30 p-6 text-center text-sm text-on-surface-variant">
            <AlertCircle className="mx-auto mb-2 h-5 w-5 opacity-50" />
            No updates yet this week.
          </div>
        ) : (
          <div className="space-y-2">
            {mine.map((u) => (
              <div key={u.dailyUpdateId} className="rounded-xl bg-surface-container border border-outline-variant/30 p-4 shadow-soft">
                <div className="flex items-start gap-3">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2">
                      <p className="font-bold text-sm truncate">{u.ticketNo}</p>
                      <span className={cn("text-[10px] font-bold uppercase tracking-wider px-2 py-1 rounded-full", STATUS_TONE[u.status])}>
                        {u.status}
                      </span>
                    </div>
                    <p className="text-sm text-on-surface-variant mt-1">{u.description}</p>
                    <p className="mt-2 text-xs text-on-surface-variant/70 flex items-center gap-1">
                      <CheckCircle2 className="h-3 w-3" /> {fmtDate(u.workDate, "long")}
                    </p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
