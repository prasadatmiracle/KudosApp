import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ListChecks, Vote, MessageSquare, AlertCircle, Loader2, Calendar, Send,
} from "lucide-react";
import { api } from "@/lib/api";
import { cn, timeAgo } from "@/lib/utils";
import { toast } from "sonner";

interface TaskItem {
  taskId: number;
  title: string;
  description?: string | null;
  taskType: "Vote" | "Action" | "Info";
  state: string;
  options?: string[] | null;
  dueAtUtc?: string | null;
  createdAtUtc: string;
}

const TYPE_ICONS = { Vote: Vote, Action: ListChecks, Info: MessageSquare } as const;

const TYPE_TONE: Record<TaskItem["taskType"], string> = {
  Vote:   "bg-primary/15            text-primary               border-primary/30",
  Action: "bg-tertiary-container/30 text-on-tertiary-container border-tertiary/30",
  Info:   "bg-secondary-container/40 text-on-secondary-container border-secondary/30",
};

export function Tasks() {
  const { data, isLoading } = useQuery<TaskItem[]>({
    queryKey: ["tasks-active"],
    queryFn: () => api<TaskItem[]>("/tasks/active"),
  });

  return (
    <div className="space-y-5">
      <header>
        <h1 className="text-2xl font-bold tracking-tight">Tasks &amp; Polls</h1>
        <p className="text-sm text-on-surface-variant mt-1">Vote, share quick updates, or acknowledge action items from your team.</p>
      </header>

      {isLoading ? (
        <div className="space-y-3">{[...Array(3)].map((_, i) => <div key={i} className="h-32 rounded-xl bg-surface-container animate-pulse" />)}</div>
      ) : !data || data.length === 0 ? (
        <div className="rounded-xl bg-surface-container border border-outline-variant/30 p-10 text-center">
          <ListChecks className="mx-auto h-10 w-10 text-on-surface-variant mb-3" />
          <p className="text-sm font-semibold">No active tasks.</p>
          <p className="text-xs text-on-surface-variant mt-1">You're all caught up — enjoy the focus time.</p>
        </div>
      ) : (
        <div className="space-y-3">
          {data.map((t) => <TaskCard key={t.taskId} task={t} />)}
        </div>
      )}
    </div>
  );
}

function TaskCard({ task }: { task: TaskItem }) {
  const Icon = TYPE_ICONS[task.taskType] ?? MessageSquare;
  const [option, setOption] = useState<string>("");
  const [remark, setRemark] = useState("");
  const qc = useQueryClient();

  const respond = useMutation({
    mutationFn: () => api(`/tasks/${task.taskId}/respond`, { method: "POST", body: { option, remark } }),
    onSuccess: () => {
      toast.success("Response saved");
      setOption(""); setRemark("");
      qc.invalidateQueries({ queryKey: ["tasks-active"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
    },
    onError: (e: any) => toast.error(e?.message ?? "Could not save response"),
  });

  return (
    <article className="rounded-xl bg-surface-container border border-outline-variant/30 p-4 shadow-soft">
      <div className="flex items-start justify-between gap-2 mb-2">
        <span className={cn(
          "inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider",
          TYPE_TONE[task.taskType]
        )}>
          <Icon className="h-3 w-3" />
          {task.taskType}
        </span>
        {task.dueAtUtc && (
          <span className="text-xs text-on-surface-variant inline-flex items-center gap-1">
            <Calendar className="h-3 w-3" /> Due {timeAgo(task.dueAtUtc)}
          </span>
        )}
      </div>
      <h3 className="font-bold leading-snug">{task.title}</h3>
      {task.description && <p className="text-sm text-on-surface-variant mt-1">{task.description}</p>}

      {task.taskType === "Vote" && task.options && task.options.length > 0 && (
        <div className="mt-4 flex flex-wrap gap-2">
          {task.options.map((opt) => (
            <button
              key={opt}
              type="button"
              onClick={() => setOption(opt)}
              className={cn(
                "rounded-full border px-4 py-1.5 text-xs font-bold transition-all",
                option === opt
                  ? "border-primary bg-primary/10 text-primary"
                  : "border-outline-variant text-on-surface-variant hover:bg-surface-container-high"
              )}
            >
              {opt}
            </button>
          ))}
        </div>
      )}

      <textarea
        rows={2}
        value={remark}
        onChange={(e) => setRemark(e.target.value)}
        placeholder={task.taskType === "Vote" ? "Add a remark (optional)…" : "Your response…"}
        className="mt-3 w-full rounded-md border border-outline-variant bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
      />

      <div className="mt-3 flex items-center gap-2">
        <button
          type="button"
          onClick={() => respond.mutate()}
          disabled={respond.isPending || (task.taskType === "Vote" && !option) || (task.taskType !== "Vote" && !remark)}
          className="inline-flex items-center gap-1.5 rounded-lg bg-primary text-on-primary px-3.5 py-2 text-xs font-bold hover:opacity-95 transition shadow-glow disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {respond.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Send className="h-3.5 w-3.5" />}
          Submit response
        </button>
        {task.taskType === "Vote" && !option && (
          <span className="text-[11px] text-on-surface-variant inline-flex items-center gap-1">
            <AlertCircle className="h-3 w-3" /> Pick an option to submit
          </span>
        )}
      </div>
    </article>
  );
}
