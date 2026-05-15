import { useState, useMemo } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Trophy, Briefcase, Check, X, Loader2 } from "lucide-react";
import { api } from "@/lib/api";
import { cn, timeAgo } from "@/lib/utils";
import { toast } from "sonner";

interface PendingItem {
  validationRecordId: number;
  entityType: "Achievement" | "SalesEnquiry";
  entityId: number;
  createdAtUtc: string;
  userName: string;
  category: string;
  title: string;
  description: string;
  proofUrl?: string | null;
}

type Filter = "All" | "Achievement" | "SalesEnquiry";

export function Validation() {
  const qc = useQueryClient();
  const [filter, setFilter] = useState<Filter>("All");
  const [selected, setSelected] = useState<Set<number>>(new Set());

  const { data, isLoading } = useQuery<PendingItem[]>({
    queryKey: ["validation-pending"],
    queryFn: () => api<PendingItem[]>("/validations/pending-detail"),
  });

  const items = useMemo(() => {
    if (!data) return [];
    return filter === "All" ? data : data.filter((d) => d.entityType === filter);
  }, [data, filter]);

  const decide = useMutation({
    mutationFn: ({ id, status }: { id: number; status: "Approved" | "Rejected" }) =>
      api(`/validations/${id}/decision`, { method: "POST", body: { status, remarks: "" } }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["validation-pending"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });

  const bulk = useMutation({
    mutationFn: (status: "Approved" | "Rejected") =>
      api("/validations/bulk", {
        method: "POST",
        body: { validationRecordIds: Array.from(selected), status, remarks: "" },
      }),
    onSuccess: (_, status) => {
      toast.success(`${selected.size} item(s) ${status.toLowerCase()}`);
      setSelected(new Set());
      qc.invalidateQueries({ queryKey: ["validation-pending"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });

  function toggle(id: number) {
    const next = new Set(selected);
    next.has(id) ? next.delete(id) : next.add(id);
    setSelected(next);
  }
  function toggleAll() {
    if (selected.size === items.length) setSelected(new Set());
    else setSelected(new Set(items.map((i) => i.validationRecordId)));
  }

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Validation Queue</h1>
        <p className="text-sm text-on-surface-variant mt-1">Review and approve pending achievements and sales targets.</p>
      </div>

      {/* Filter tabs */}
      <div className="flex items-center gap-1 border-b border-outline-variant">
        {(["All", "Achievement", "SalesEnquiry"] as Filter[]).map((f) => (
          <button
            key={f}
            onClick={() => { setFilter(f); setSelected(new Set()); }}
            className={cn(
              "px-4 py-2.5 text-sm font-bold tracking-tight transition relative",
              filter === f
                ? "text-primary"
                : "text-on-surface-variant hover:text-on-surface"
            )}
          >
            {f === "SalesEnquiry" ? "Sales" : f === "Achievement" ? "Achievements" : "All"}
            {filter === f && (
              <span className="absolute -bottom-px left-0 right-0 h-0.5 rounded-full bg-primary" />
            )}
          </button>
        ))}
      </div>

      {/* Bulk action header */}
      {items.length > 0 && (
        <div className="rounded-xl bg-surface-container border border-outline-variant/30 p-3 flex items-center gap-3 flex-wrap">
          <label className="inline-flex items-center gap-2 text-xs font-semibold text-on-surface-variant cursor-pointer">
            <input
              type="checkbox"
              checked={selected.size === items.length}
              onChange={toggleAll}
              className="h-4 w-4 rounded border-outline-variant accent-primary"
            />
            <span className="hidden sm:inline">Select all {items.length} pending</span>
            <span className="sm:hidden">All ({items.length})</span>
          </label>
          <div className="flex-1" />
          <button
            disabled={selected.size === 0 || bulk.isPending}
            onClick={() => bulk.mutate("Rejected")}
            className="inline-flex items-center gap-1.5 rounded-full border border-outline-variant px-3 py-1.5 text-xs font-bold hover:bg-error-container/20 hover:text-error hover:border-error/30 disabled:opacity-40 disabled:pointer-events-none transition"
          >
            <X className="h-3.5 w-3.5" /> Reject {selected.size > 0 && `(${selected.size})`}
          </button>
          <button
            disabled={selected.size === 0 || bulk.isPending}
            onClick={() => bulk.mutate("Approved")}
            className="inline-flex items-center gap-1.5 rounded-full bg-primary text-on-primary px-3 py-1.5 text-xs font-bold hover:opacity-95 disabled:opacity-40 disabled:pointer-events-none transition shadow-glow"
          >
            {bulk.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Check className="h-3.5 w-3.5" />}
            Approve {selected.size > 0 && `(${selected.size})`}
          </button>
        </div>
      )}

      {/* List */}
      {isLoading ? (
        <div className="space-y-2">
          {[...Array(3)].map((_, i) => <div key={i} className="h-32 rounded-xl bg-surface-container animate-pulse" />)}
        </div>
      ) : items.length === 0 ? (
        <div className="rounded-xl bg-surface-container border border-outline-variant/30 p-10 text-center">
          <Check className="mx-auto h-10 w-10 text-success mb-3" />
          <p className="text-sm font-semibold">All caught up.</p>
          <p className="text-xs text-on-surface-variant mt-1">Nothing pending review.</p>
        </div>
      ) : (
        <div className="space-y-3">
          {items.map((item) => {
            const isAch = item.entityType === "Achievement";
            const Icon = isAch ? Trophy : Briefcase;
            const isSel = selected.has(item.validationRecordId);
            return (
              <article
                key={item.validationRecordId}
                className={cn(
                  "rounded-xl bg-surface-container border p-4 shadow-soft transition-colors",
                  isSel ? "border-primary/50 ring-2 ring-primary/20" : "border-outline-variant/30"
                )}
              >
                <div className="flex items-start gap-3">
                  <input
                    type="checkbox"
                    checked={isSel}
                    onChange={() => toggle(item.validationRecordId)}
                    className="mt-1 h-4 w-4 rounded border-outline-variant accent-primary"
                  />
                  <div className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-tertiary-container/30 text-tertiary">
                    <Icon className="h-4 w-4" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-2">
                      <h3 className="font-bold text-sm leading-snug">{item.title}</h3>
                      <span className={cn(
                        "text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full shrink-0",
                        isAch ? "bg-secondary-container/40 text-on-secondary-container" : "bg-primary-container/30 text-primary"
                      )}>
                        {isAch ? "Achievement" : "Sales"}
                      </span>
                    </div>
                    <p className="text-xs text-on-surface-variant mt-1">
                      <span className="font-medium">{item.userName}</span> · {timeAgo(item.createdAtUtc)}
                    </p>
                    <p className="mt-2 text-sm text-on-surface-variant line-clamp-2">{item.description}</p>

                    <div className="mt-3 flex items-center gap-2">
                      <button
                        onClick={() => decide.mutate({ id: item.validationRecordId, status: "Rejected" })}
                        disabled={decide.isPending}
                        className="flex-1 rounded-lg border border-outline-variant px-3 py-2 text-xs font-bold hover:bg-error-container/20 hover:text-error hover:border-error/30 transition"
                      >
                        Reject
                      </button>
                      <button
                        onClick={() => decide.mutate({ id: item.validationRecordId, status: "Approved" })}
                        disabled={decide.isPending}
                        className="flex-1 rounded-lg bg-primary text-on-primary px-3 py-2 text-xs font-bold hover:opacity-95 transition shadow-glow"
                      >
                        Approve
                      </button>
                    </div>
                  </div>
                </div>
              </article>
            );
          })}
        </div>
      )}
    </div>
  );
}
