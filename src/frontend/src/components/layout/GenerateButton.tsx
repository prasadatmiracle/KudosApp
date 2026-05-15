import { useLocation } from "react-router-dom";
import { Sparkles, Loader2 } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { cn } from "@/lib/utils";

/**
 * Context-aware "Generate" action. See WISHLIST.md P3 — full plan.
 *
 * Phase 1 (now):  only renders on pages where an action is wired.
 *                 Currently: Reports → opens "generate report" coming-soon toast.
 * Phase 2 (next): Feed → AI weekly narrative. Daily → AI-fill from inbox tasks.
 * Phase 3:        Validation → auto-approve suggestions. Achievements → drafts.
 */

interface Action {
  label: string;
  /** Run the action; return a promise so the button can show a loading state */
  run: () => Promise<void>;
}

const ACTIONS: Record<string, Action | undefined> = {
  "/reports": {
    label: "Generate report",
    run: async () => {
      // WISHLIST.md P3: wire to POST /api/reports/{weekly|monthly|quarterly}/generate
      toast.info("Report generation flow coming soon", {
        description: "Pick a period (Weekly · Monthly · Quarterly) and we'll draft it for you.",
      });
    },
  },
  "/feed": {
    label: "AI weekly summary",
    run: async () => {
      toast.info("AI weekly narrative coming soon", {
        description: "Will summarise the team's last 7 days into a shareable digest.",
      });
    },
  },
  "/daily": {
    label: "Draft from inbox",
    run: async () => {
      toast.info("AI draft coming soon", {
        description: "Will pre-fill today's check-in based on your confirmed inbox tasks.",
      });
    },
  },
};

export function GenerateButton() {
  const { pathname } = useLocation();
  const [busy, setBusy] = useState(false);

  const action = ACTIONS[pathname];
  if (!action) return null;

  async function onClick() {
    if (!action) return;
    setBusy(true);
    try { await action.run(); }
    finally { setBusy(false); }
  }

  return (
    <button
      onClick={onClick}
      disabled={busy}
      className={cn(
        "hidden sm:inline-flex items-center gap-1.5 rounded-full bg-grad-primary text-on-primary px-3.5 py-1.5 text-xs font-semibold shadow-glow hover:opacity-95 transition active:scale-95 disabled:opacity-60"
      )}
    >
      {busy ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Sparkles className="h-3.5 w-3.5" />}
      {action.label}
    </button>
  );
}
