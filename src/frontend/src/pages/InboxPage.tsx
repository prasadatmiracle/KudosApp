import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Mail, MessageSquare, Sparkles, Check, X, Inbox as InboxIcon,
  Calendar, AlertCircle, MoreVertical, Loader2,
} from "lucide-react";
import { api } from "@/lib/api";
import { cn, timeAgo } from "@/lib/utils";
import { toast } from "sonner";

interface InboxTask {
  inboxTaskId: number;
  sourceChannel: "ZohoMail" | "ZohoCliq";
  sourceSender: string;
  extractedTaskText: string;
  category?: string | null;
  priority?: string | null;
  dueAtUtc?: string | null;
  state: string;
  createdAtUtc: string;
}

type Tab = "pending" | "active";

const PRIORITY_TONE: Record<string, string> = {
  Critical: "bg-error-container/40 text-on-error-container border-error/30",
  High:     "bg-error-container/30 text-error border-error/30",
  Medium:   "bg-tertiary-container/30 text-on-tertiary-container border-tertiary/30",
  Low:      "bg-surface-container-high text-on-surface-variant border-outline-variant",
};

export function InboxPage() {
  const qc = useQueryClient();
  const [tab, setTab] = useState<Tab>("pending");

  const pending = useQuery<InboxTask[]>({
    queryKey: ["inbox-pending"],
    queryFn: () => api<InboxTask[]>("/inbox-tasks/pending"),
  });
  const active = useQuery<InboxTask[]>({
    queryKey: ["inbox-active"],
    queryFn: () => api<InboxTask[]>("/inbox-tasks"),
  });

  const confirm = useMutation({
    mutationFn: (id: number) =>
      api(`/inbox-tasks/${id}/confirm`, {
        method: "POST",
        body: { category: "FollowUp", priority: "Medium", dueAtUtc: null },
      }),
    onSuccess: () => {
      toast.success("Task confirmed");
      qc.invalidateQueries({ queryKey: ["inbox-pending"] });
      qc.invalidateQueries({ queryKey: ["inbox-active"] });
    },
  });
  const dismiss = useMutation({
    mutationFn: (id: number) => api(`/inbox-tasks/${id}/dismiss`, { method: "POST" }),
    onSuccess: () => {
      toast.success("Task dismissed");
      qc.invalidateQueries({ queryKey: ["inbox-pending"] });
    },
  });

  const pendingCount = pending.data?.length ?? 0;
  const confirmedToday = active.data?.length ?? 0;

  return (
    <div className="space-y-5">
      {/* Tab header */}
      <div className="flex border-b border-outline-variant">
        {([
          { id: "pending" as Tab, label: "Pending Confirmation", count: pendingCount },
          { id: "active"  as Tab, label: "Active",               count: confirmedToday },
        ]).map((t) => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            className={cn(
              "flex-1 py-3 text-sm font-bold tracking-tight transition relative",
              tab === t.id ? "text-primary" : "text-on-surface-variant hover:text-on-surface"
            )}
          >
            {t.label}{t.count > 0 && <span className="ml-1 text-xs opacity-80">({t.count})</span>}
            {tab === t.id && (
              <span className="absolute -bottom-px left-0 right-0 h-0.5 rounded-full bg-primary" />
            )}
          </button>
        ))}
      </div>

      {tab === "pending" ? (
        <>
          {/* Momentum card */}
          <section className="rounded-xl bg-surface-container border border-outline-variant/30 p-4 shadow-soft">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-[10px] font-bold uppercase tracking-wider text-on-surface-variant">Daily Momentum</p>
                <p className="text-2xl font-bold mt-1">{pendingCount}</p>
                <p className="text-xs text-on-surface-variant">Pending validations</p>
              </div>
              <span className="text-xs text-on-surface-variant">Confirmed Today {confirmedToday}/{pendingCount + confirmedToday || 1}</span>
            </div>
            <div className="mt-3 h-1.5 rounded-full bg-surface-container-high overflow-hidden">
              <div
                className="h-full bg-grad-primary rounded-full transition-all"
                style={{ width: `${pendingCount + confirmedToday > 0 ? (confirmedToday / (pendingCount + confirmedToday)) * 100 : 0}%` }}
              />
            </div>
          </section>

          {pending.isLoading ? (
            <Skeletons />
          ) : pendingCount === 0 ? (
            <EmptyState icon={InboxIcon} title="Inbox zero." subtitle="No tasks pending confirmation." />
          ) : (
            <div className="space-y-3">
              {pending.data?.map((t) => <PendingCard
                key={t.inboxTaskId}
                task={t}
                onConfirm={() => confirm.mutate(t.inboxTaskId)}
                onDismiss={() => dismiss.mutate(t.inboxTaskId)}
                busy={confirm.isPending || dismiss.isPending}
              />)}
            </div>
          )}
        </>
      ) : (
        <>
          <h3 className="text-2xl font-bold tracking-tight">Active Focus</h3>
          {active.isLoading ? (
            <Skeletons />
          ) : confirmedToday === 0 ? (
            <EmptyState icon={Check} title="No active tasks." subtitle="Confirm a pending task to get started." />
          ) : (
            <div className="space-y-2">
              {active.data?.map((t) => <ActiveCard key={t.inboxTaskId} task={t} />)}
            </div>
          )}
        </>
      )}
    </div>
  );
}

function PendingCard({ task, onConfirm, onDismiss, busy }: { task: InboxTask; onConfirm: () => void; onDismiss: () => void; busy: boolean }) {
  const isMail = task.sourceChannel === "ZohoMail";
  const Icon   = isMail ? Mail : MessageSquare;
  return (
    <article className="rounded-xl bg-surface-container border border-outline-variant/30 p-4 shadow-soft">
      <header className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-on-surface-variant">
          <Icon className="h-4 w-4" />
          {isMail ? "Email Source" : "Cliq Message"} · <span className="text-on-surface normal-case">{task.sourceSender}</span>
        </div>
        <span className="inline-flex items-center gap-1 rounded-full bg-primary/15 px-2 py-0.5 text-[10px] font-bold text-primary">
          <Sparkles className="h-3 w-3" />
          AI Extracted
        </span>
      </header>
      <p className="text-sm text-on-surface leading-relaxed">"{task.extractedTaskText}"</p>
      <div className="mt-4 flex gap-2">
        <button
          onClick={onConfirm}
          disabled={busy}
          className="flex-1 inline-flex items-center justify-center gap-1.5 rounded-lg bg-primary text-on-primary px-3 py-2 text-xs font-bold hover:opacity-95 transition shadow-glow disabled:opacity-50"
        >
          {busy ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Check className="h-3.5 w-3.5" />}
          Confirm Task
        </button>
        <button
          onClick={onDismiss}
          disabled={busy}
          className="flex-1 inline-flex items-center justify-center gap-1.5 rounded-lg border border-outline-variant px-3 py-2 text-xs font-bold hover:bg-error-container/20 hover:text-error transition disabled:opacity-50"
        >
          <X className="h-3.5 w-3.5" />
          Dismiss
        </button>
      </div>
    </article>
  );
}

function ActiveCard({ task }: { task: InboxTask }) {
  const tone = task.priority ? PRIORITY_TONE[task.priority] ?? PRIORITY_TONE.Low : PRIORITY_TONE.Low;
  return (
    <article className="rounded-xl bg-surface-container border border-outline-variant/30 p-4 shadow-soft">
      <div className="flex items-start justify-between gap-2">
        <span className={cn("inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider", tone)}>
          {task.priority ?? "Routine"}
        </span>
        <button className="text-on-surface-variant hover:text-on-surface transition-colors">
          <MoreVertical className="h-4 w-4" />
        </button>
      </div>
      <p className="font-bold mt-2 leading-snug">{task.extractedTaskText}</p>
      <p className="mt-2 text-xs text-on-surface-variant flex items-center gap-1">
        {task.dueAtUtc
          ? <><Calendar className="h-3 w-3" /> Due {timeAgo(task.dueAtUtc)}</>
          : <><AlertCircle className="h-3 w-3" /> No due date set</>
        }
      </p>
    </article>
  );
}

function Skeletons() {
  return (
    <div className="space-y-3">
      {[...Array(3)].map((_, i) => <div key={i} className="h-32 rounded-xl bg-surface-container animate-pulse" />)}
    </div>
  );
}

function EmptyState({ icon: Icon, title, subtitle }: { icon: any; title: string; subtitle: string }) {
  return (
    <div className="rounded-xl bg-surface-container border border-outline-variant/30 p-10 text-center">
      <Icon className="mx-auto h-10 w-10 text-on-surface-variant mb-3" />
      <p className="text-sm font-semibold">{title}</p>
      <p className="text-xs text-on-surface-variant mt-1">{subtitle}</p>
    </div>
  );
}
