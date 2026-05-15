import { useState, type FormEvent, useEffect } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CheckCircle2, AlertCircle, Loader2 } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { api } from "@/lib/api";
import { fmtDate } from "@/lib/utils";
import { toast } from "sonner";

interface DailyUpdate {
  dailyUpdateId: number;
  workDate: string;
  ticketNo: string;
  description: string;
  status: string;
}

const STATUS_OPTIONS = ["Open", "InProgress", "Completed", "Blocked", "NoTask"] as const;

const STATUS_TONE: Record<string, "success" | "warning" | "destructive" | "secondary"> = {
  Completed:  "success",
  InProgress: "warning",
  Blocked:    "destructive",
  Open:       "secondary",
  NoTask:     "secondary",
};

export function Daily() {
  const qc = useQueryClient();
  const [ticketNo, setTicket] = useState("");
  const [description, setDesc] = useState("");
  const [status, setStatus] = useState<typeof STATUS_OPTIONS[number]>("InProgress");

  const { data: mine, isLoading } = useQuery<DailyUpdate[]>({
    queryKey: ["daily-mine"],
    queryFn: () => api<DailyUpdate[]>("/daily-updates/my?days=7"),
  });

  const submit = useMutation({
    mutationFn: (body: { ticketNo: string; description: string; status: string }) =>
      api("/daily-updates", { method: "POST", body }),
    onSuccess: () => {
      toast.success("Daily update saved");
      setTicket(""); setDesc("");
      qc.invalidateQueries({ queryKey: ["daily-mine"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
    },
    onError: (e: any) => toast.error(e?.message ?? "Could not save"),
  });

  // Auto-restore draft
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
    } else {
      localStorage.removeItem("kudos.daily.draft");
    }
  }, [ticketNo, description, status]);

  function onSubmit(e: FormEvent) {
    e.preventDefault();
    submit.mutate({ ticketNo, description, status });
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Submit today's update</CardTitle>
          <CardDescription>Takes under 15 seconds. Drafts auto-save.</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={onSubmit} className="space-y-4">
            <div className="grid sm:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="ticket">Ticket / Project</Label>
                <Input id="ticket" placeholder="PROJ-123" value={ticketNo} onChange={(e) => setTicket(e.target.value)} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="status">Status</Label>
                <select
                  id="status"
                  value={status}
                  onChange={(e) => setStatus(e.target.value as typeof STATUS_OPTIONS[number])}
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                >
                  {STATUS_OPTIONS.map((s) => <option key={s} value={s}>{s}</option>)}
                </select>
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="desc">Description</Label>
              <Textarea id="desc" placeholder="What did you work on?" value={description} onChange={(e) => setDesc(e.target.value)} required />
            </div>
            <div className="flex items-center gap-3">
              <Button type="submit" disabled={submit.isPending}>
                {submit.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
                {submit.isPending ? "Saving…" : "Submit update"}
              </Button>
              {(ticketNo || description) && (
                <span className="text-xs text-muted-foreground inline-flex items-center gap-1">
                  <CheckCircle2 className="h-3 w-3 text-emerald-600" />
                  Draft auto-saved
                </span>
              )}
            </div>
          </form>
        </CardContent>
      </Card>

      <section>
        <h3 className="mb-3 text-xs font-bold uppercase tracking-wider text-muted-foreground">My last 7 days</h3>
        {isLoading ? (
          <p className="text-sm text-muted-foreground">Loading…</p>
        ) : !mine || mine.length === 0 ? (
          <Card><CardContent className="p-6 text-center text-sm text-muted-foreground">
            <AlertCircle className="mx-auto mb-2 h-5 w-5 opacity-50" />
            No updates yet this week.
          </CardContent></Card>
        ) : (
          <div className="space-y-2">
            {mine.map((u) => (
              <Card key={u.dailyUpdateId}>
                <CardContent className="flex items-start gap-3 p-4">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2">
                      <p className="font-semibold text-sm truncate">{u.ticketNo}</p>
                      <Badge variant={STATUS_TONE[u.status] ?? "secondary"}>{u.status}</Badge>
                    </div>
                    <p className="text-sm text-muted-foreground mt-1">{u.description}</p>
                    <p className="mt-2 text-xs text-muted-foreground">{fmtDate(u.workDate, "long")}</p>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
