import { useQuery } from "@tanstack/react-query";
import { Crown, Medal, Award, TrendingUp, Sparkles } from "lucide-react";
import { api } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { cn, initials } from "@/lib/utils";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";

interface RawRow { userId: number; name: string; points: number; }
interface Row extends RawRow { rank: number; }

/**
 * SCR-1 C19 (from Assessment A4): show top-10 only, plus the user's own
 * rank card if they're outside the top 10. Removes the "visible losers"
 * problem of a full 50-person ranking.
 */
export function Leaderboard() {
  const { user, isManager } = useAuth();
  const now = new Date();
  const { data: raw, isLoading } = useQuery<RawRow[]>({
    queryKey: ["leaderboard", now.getFullYear(), now.getMonth() + 1],
    queryFn: () => api<RawRow[]>(`/performance/leaderboard?year=${now.getFullYear()}&month=${now.getMonth() + 1}`),
  });

  const allRows: Row[] = (raw ?? []).map((r, i) => ({ ...r, rank: i + 1 }));
  // Managers/Admins see full list (SCR C19 AC 7); everyone else sees top 10 + self
  const visible = isManager ? allRows : allRows.slice(0, 10);
  const myRow = allRows.find((r) => r.userId === user?.userId);
  const showMyRowSeparately = myRow && !visible.some((r) => r.userId === myRow.userId);

  return (
    <div className="space-y-5">
      {/* Hero */}
      <div className="rounded-2xl bg-grad-primary text-on-primary p-5 shadow-glow relative overflow-hidden">
        <div className="absolute -right-6 -top-6 h-32 w-32 rounded-full bg-white/10 blur-2xl" />
        <p className="text-xs font-bold uppercase tracking-wider text-on-primary/80">This Month</p>
        <h2 className="text-2xl font-bold tracking-tight mt-1">Top performers</h2>
        <p className="text-sm text-on-primary/85 mt-1">Earn points by submitting daily updates, voting, and posting achievements.</p>
      </div>

      {/* Your rank card — always shows */}
      {myRow && (
        <section className="rounded-xl bg-surface-container border border-primary/40 p-4 shadow-soft">
          <div className="flex items-center gap-4">
            <div className="grid h-12 w-12 place-items-center rounded-xl bg-grad-primary text-on-primary shadow-glow">
              <Sparkles className="h-5 w-5" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-[10px] font-bold uppercase tracking-wider text-on-surface-variant">Your position</p>
              <p className="text-xl font-bold tracking-tight">
                Rank #{myRow.rank} · <span className="bg-grad-primary bg-clip-text text-transparent">{myRow.points}</span>
                <span className="text-sm text-on-surface-variant font-medium ml-1">pts</span>
              </p>
            </div>
            <TrendingUp className="h-5 w-5 text-success" />
          </div>
        </section>
      )}

      {/* Top 10 */}
      {isLoading ? (
        <div className="space-y-2">{[...Array(5)].map((_, i) => <div key={i} className="h-16 rounded-xl bg-surface-container animate-pulse" />)}</div>
      ) : allRows.length === 0 ? (
        <div className="rounded-xl bg-surface-container border border-outline-variant/30 p-8 text-center text-sm text-on-surface-variant">
          No leaderboard data yet this month.
        </div>
      ) : (
        <>
          <h3 className="text-xs font-bold uppercase tracking-wider text-on-surface-variant">
            {isManager ? `Full ranking · ${allRows.length} members` : "Top 10"}
          </h3>
          <div className="space-y-2">
            {visible.map((row) => {
              const isMe = row.userId === user?.userId;
              return (
                <article
                  key={row.userId}
                  className={cn(
                    "rounded-xl border p-3 shadow-soft flex items-center gap-3 transition-colors",
                    isMe
                      ? "bg-primary/10 border-primary/50 ring-1 ring-primary/30"
                      : "bg-surface-container border-outline-variant/30 hover:bg-surface-container-high"
                  )}
                >
                  <RankBadge rank={row.rank} />
                  <Avatar className="h-9 w-9 text-sm">
                    <AvatarFallback>{initials(row.name)}</AvatarFallback>
                  </Avatar>
                  <p className={cn("flex-1 min-w-0 truncate font-semibold", isMe && "text-primary")}>
                    {row.name}{isMe && " (you)"}
                  </p>
                  <span className="text-xl font-bold tracking-tight bg-grad-primary bg-clip-text text-transparent tabular-nums">
                    {row.points}
                  </span>
                </article>
              );
            })}
            {showMyRowSeparately && myRow && (
              <>
                <div className="text-center text-on-surface-variant text-xs py-1">···</div>
                <article className="rounded-xl bg-primary/10 border border-primary/50 ring-1 ring-primary/30 p-3 shadow-soft flex items-center gap-3">
                  <RankBadge rank={myRow.rank} />
                  <Avatar className="h-9 w-9 text-sm">
                    <AvatarFallback>{initials(myRow.name)}</AvatarFallback>
                  </Avatar>
                  <p className="flex-1 min-w-0 truncate font-semibold text-primary">{myRow.name} (you)</p>
                  <span className="text-xl font-bold tracking-tight bg-grad-primary bg-clip-text text-transparent tabular-nums">
                    {myRow.points}
                  </span>
                </article>
              </>
            )}
          </div>
        </>
      )}
    </div>
  );
}

function RankBadge({ rank }: { rank: number }) {
  if (rank === 1) return <Crown className="h-6 w-6 text-amber-400" />;
  if (rank === 2) return <Medal className="h-6 w-6 text-slate-300" />;
  if (rank === 3) return <Award className="h-6 w-6 text-amber-600" />;
  return <span className="w-6 text-sm font-bold text-on-surface-variant text-center">#{rank}</span>;
}
