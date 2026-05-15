import { useQuery } from "@tanstack/react-query";
import { Crown, Medal, Award } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Skeleton } from "@/components/ui/skeleton";
import { api } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { cn, initials } from "@/lib/utils";

interface RawRow { userId: number; name: string; points: number; }
interface Row extends RawRow { rank: number; }

export function Leaderboard() {
  const { user } = useAuth();
  const now = new Date();
  const { data: raw, isLoading } = useQuery<RawRow[]>({
    queryKey: ["leaderboard", now.getFullYear(), now.getMonth() + 1],
    queryFn: () => api<RawRow[]>(`/performance/leaderboard?year=${now.getFullYear()}&month=${now.getMonth() + 1}`),
  });
  const data: Row[] | undefined = raw?.map((r, i) => ({ ...r, rank: i + 1 }));

  return (
    <div className="space-y-4">
      <Card className="bg-grad-topbar text-white border-0">
        <CardContent className="p-5">
          <p className="text-xs font-semibold uppercase tracking-wider text-white/70">This Month</p>
          <h2 className="mt-1 text-2xl font-bold tracking-tight">Top performers</h2>
          <p className="mt-1 text-sm text-white/80">Earn points by submitting daily updates, voting, and posting achievements.</p>
        </CardContent>
      </Card>

      {isLoading ? (
        <div className="space-y-2">
          {[...Array(5)].map((_, i) => <Skeleton key={i} className="h-16 rounded-xl" />)}
        </div>
      ) : !data || data.length === 0 ? (
        <Card><CardContent className="p-8 text-center text-sm text-muted-foreground">No leaderboard data yet.</CardContent></Card>
      ) : (
        <div className="space-y-2">
          {data.map((row) => {
            const isMe = row.userId === user?.userId;
            return (
              <Card
                key={row.userId}
                className={cn(isMe && "border-2 border-primary bg-primary/5 ring-2 ring-primary/10")}
              >
                <CardContent className="flex items-center gap-4 p-4">
                  <div className="w-10 flex justify-center">
                    <RankBadge rank={row.rank} />
                  </div>
                  <Avatar className="h-9 w-9 text-sm">
                    <AvatarFallback>{initials(row.name)}</AvatarFallback>
                  </Avatar>
                  <div className="flex-1 min-w-0">
                    <p className={cn("font-semibold truncate", isMe && "text-primary")}>{row.name}{isMe && " (you)"}</p>
                  </div>
                  <p className="text-xl font-bold tracking-tight bg-grad-brand bg-clip-text text-transparent tabular-nums">
                    {row.points}
                  </p>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}

function RankBadge({ rank }: { rank: number }) {
  if (rank === 1) return <Crown className="h-6 w-6 text-amber-500" />;
  if (rank === 2) return <Medal className="h-6 w-6 text-slate-400" />;
  if (rank === 3) return <Award className="h-6 w-6 text-amber-700" />;
  return <span className="text-sm font-bold text-muted-foreground">#{rank}</span>;
}
