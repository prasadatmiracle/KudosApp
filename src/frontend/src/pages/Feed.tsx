import { useQuery } from "@tanstack/react-query";
import {
  Trophy, PartyPopper, ClipboardList, Briefcase, Users, CheckCircle2, Circle,
} from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { api } from "@/lib/api";
import { fmtDate } from "@/lib/utils";

interface FeedItem {
  kind: "Achievement" | "Event" | "DailyUpdate" | "SalesEnquiry" | "Meeting" | "Task";
  title: string;
  description?: string;
  authorName?: string;
  createdAtUtc: string;
}

const ICON_MAP = {
  Achievement:  Trophy,
  Event:        PartyPopper,
  DailyUpdate:  ClipboardList,
  SalesEnquiry: Briefcase,
  Meeting:      Users,
  Task:         CheckCircle2,
} as const;

export function Feed() {
  const { data, isLoading } = useQuery<FeedItem[]>({
    queryKey: ["feed"],
    queryFn: () => api<FeedItem[]>("/feed?page=1&pageSize=30"),
  });

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[...Array(5)].map((_, i) => (
          <Skeleton key={i} className="h-24 rounded-xl" />
        ))}
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <Card>
        <CardContent className="p-10 text-center text-sm text-on-surface-variant">
          No feed activity yet.
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-3">
      {data.map((item, idx) => {
        const Icon = ICON_MAP[item.kind] ?? Circle;
        return (
          <Card key={idx} className="hover:-translate-y-px transition-transform">
            <CardContent className="flex items-start gap-3 p-4">
              <div className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-primary/10 text-primary">
                <Icon className="h-5 w-5" />
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-start justify-between gap-2">
                  <p className="font-semibold leading-snug">{item.title}</p>
                  <Badge variant="outline" className="text-[10px] uppercase tracking-wider">{item.kind}</Badge>
                </div>
                {item.description && (
                  <p className="mt-1 text-sm text-on-surface-variant line-clamp-2">{item.description}</p>
                )}
                <div className="mt-2 flex flex-wrap gap-x-3 gap-y-1 text-xs text-on-surface-variant">
                  {item.authorName && <span>{item.authorName}</span>}
                  <span>{fmtDate(item.createdAtUtc, "long")}</span>
                </div>
              </div>
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}
