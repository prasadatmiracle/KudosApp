import { useQuery } from "@tanstack/react-query";
import { Sparkles, Trophy, Mail, Shield } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { api } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { initials, fmtDate } from "@/lib/utils";

interface PerfData { points: number; badges: string[]; }
interface AchItem  { achievementId: number; title: string; category: string; createdAtUtc: string; validationStatus: string; }

export function Profile() {
  const { user } = useAuth();
  const { data: perf, isLoading: pLoad } = useQuery<PerfData>({
    queryKey: ["perf-my"],
    queryFn: () => api<PerfData>("/performance/my"),
  });
  const { data: ach } = useQuery<AchItem[]>({
    queryKey: ["my-achievements"],
    queryFn: () => api<AchItem[]>("/achievements/feed?page=1&pageSize=5"),
  });

  if (!user) return null;

  return (
    <div className="space-y-6">
      {/* Hero */}
      <Card>
        <CardContent className="p-6 flex flex-col items-center text-center gap-3">
          <Avatar className="h-20 w-20 text-2xl">
            <AvatarFallback>{initials(user.name)}</AvatarFallback>
          </Avatar>
          <div>
            <h2 className="text-xl font-bold">{user.name}</h2>
            <div className="mt-2 flex flex-wrap items-center justify-center gap-2">
              <Badge variant="violet"><Shield className="mr-1 h-3 w-3" />{user.role}</Badge>
              <Badge variant="secondary"><Mail className="mr-1 h-3 w-3" />{user.email}</Badge>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Stats */}
      <div className="grid grid-cols-2 gap-3">
        {pLoad ? (
          <>
            <Skeleton className="h-24 rounded-xl" />
            <Skeleton className="h-24 rounded-xl" />
          </>
        ) : (
          <>
            <Card className="bg-gradient-to-br from-primary/10 to-primary/5 border-primary/20">
              <CardContent className="p-4">
                <Sparkles className="h-5 w-5 text-primary" />
                <p className="mt-3 text-2xl font-bold tracking-tight">{perf?.points ?? 0}</p>
                <p className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Points this month</p>
              </CardContent>
            </Card>
            <Card className="bg-gradient-to-br from-violet-500/10 to-violet-500/5 border-violet-500/20">
              <CardContent className="p-4">
                <Trophy className="h-5 w-5 text-violet-600" />
                <p className="mt-3 text-2xl font-bold tracking-tight text-violet-600">{perf?.badges.length ?? 0}</p>
                <p className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Badges earned</p>
              </CardContent>
            </Card>
          </>
        )}
      </div>

      {/* Badges */}
      {perf && perf.badges.length > 0 && (
        <section>
          <h3 className="mb-3 text-xs font-bold uppercase tracking-wider text-muted-foreground">Badges</h3>
          <div className="flex flex-wrap gap-2">
            {perf.badges.map((b) => <Badge key={b} variant="violet">{b}</Badge>)}
          </div>
        </section>
      )}

      {/* Recent achievements */}
      {ach && ach.length > 0 && (
        <section>
          <h3 className="mb-3 text-xs font-bold uppercase tracking-wider text-muted-foreground">Recent achievements</h3>
          <div className="space-y-2">
            {ach.map((a) => (
              <Card key={a.achievementId}>
                <CardContent className="flex items-start gap-3 p-4">
                  <div className="grid h-10 w-10 place-items-center rounded-xl bg-primary/10 text-primary">
                    <Trophy className="h-5 w-5" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-medium truncate">{a.title}</p>
                    <p className="text-xs text-muted-foreground">{a.category} · {fmtDate(a.createdAtUtc, "long")}</p>
                  </div>
                  <Badge variant={a.validationStatus === "Approved" ? "success" : a.validationStatus === "Rejected" ? "destructive" : "warning"}>
                    {a.validationStatus}
                  </Badge>
                </CardContent>
              </Card>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
