import { useState, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Trophy, Briefcase, Sparkles, Calendar, Clock, MapPin, Plus, FileImage,
} from "lucide-react";
import { api } from "@/lib/api";
import { cn, fmtDate } from "@/lib/utils";
import { useAuth } from "@/lib/auth";

interface EventItem {
  eventId: number;
  title: string;
  description?: string | null;
  eventDate: string;
  location?: string | null;
  media: string[];
}

type Filter = "All" | "Milestones" | "Wins" | "Deadlines";

// Map title keywords → categorisation + visual tone
function categorise(e: EventItem): { kind: "CELEBRATION" | "CRITICAL" | "CLIENT WIN" | "MILESTONE"; tone: string; icon: any } {
  const t = (e.title + " " + (e.description ?? "")).toLowerCase();
  if (/award|celebrat|all-?hands|kudos/.test(t))     return { kind: "CELEBRATION", tone: "tertiary", icon: Trophy };
  if (/deadline|critical|finaliz|sign-?off/.test(t)) return { kind: "CRITICAL",    tone: "error",    icon: Clock };
  if (/client|win|deal|sign/.test(t))                return { kind: "CLIENT WIN",  tone: "primary",  icon: Briefcase };
  return { kind: "MILESTONE", tone: "secondary", icon: Sparkles };
}

function isToday(date: string) {
  const today = new Date(); today.setHours(0,0,0,0);
  const d = new Date(date);
  return d.toDateString() === today.toDateString();
}

export function Events() {
  const { isManager } = useAuth();
  const [filter, setFilter] = useState<Filter>("All");
  const { data, isLoading } = useQuery<EventItem[]>({
    queryKey: ["events-feed"],
    queryFn: () => api<EventItem[]>("/events/feed?page=1&pageSize=30"),
  });

  const filtered = useMemo(() => {
    if (!data) return [];
    if (filter === "All") return data;
    return data.filter((e) => {
      const c = categorise(e).kind;
      if (filter === "Milestones") return c === "MILESTONE" || c === "CELEBRATION";
      if (filter === "Wins")       return c === "CLIENT WIN";
      if (filter === "Deadlines")  return c === "CRITICAL";
      return true;
    });
  }, [data, filter]);

  const today    = filtered.filter((e) => isToday(e.eventDate));
  const upcoming = filtered.filter((e) => new Date(e.eventDate) > new Date()).slice(0, 10);

  return (
    <div className="space-y-5 relative">
      {/* Hero */}
      <header>
        <h1 className="text-2xl font-bold tracking-tight">Team Events</h1>
        <p className="text-sm text-on-surface-variant mt-1">Stay updated with team milestones and client wins.</p>
      </header>

      {/* Filter pills */}
      <div className="flex gap-2 overflow-x-auto scrollbar-hide pb-1">
        {(["All", "Milestones", "Wins", "Deadlines"] as Filter[]).map((f) => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            className={cn(
              "inline-flex items-center gap-1.5 rounded-full px-4 py-1.5 text-xs font-bold whitespace-nowrap transition border shrink-0",
              filter === f
                ? "bg-primary text-on-primary border-primary shadow-glow"
                : "bg-surface-container border-outline-variant/30 text-on-surface-variant hover:bg-surface-container-high"
            )}
          >
            {f === "All" && <span className="opacity-80">{filter === f ? "✓" : ""}</span>}
            {f}
          </button>
        ))}
      </div>

      {isLoading ? (
        <div className="space-y-3">
          {[...Array(3)].map((_, i) => <div key={i} className="h-32 rounded-xl bg-surface-container animate-pulse" />)}
        </div>
      ) : filtered.length === 0 ? (
        <EmptyState />
      ) : (
        <>
          {/* TODAY */}
          {today.length > 0 && (
            <section>
              <div className="flex items-center justify-between mb-3">
                <h3 className="text-xs font-bold uppercase tracking-wider text-on-surface-variant">Today</h3>
                <span className="text-[10px] font-bold tracking-wider text-on-surface-variant bg-surface-container-high px-2 py-0.5 rounded-full">
                  {new Date().toLocaleDateString("en-IN", { day: "numeric", month: "short" }).toUpperCase()}
                </span>
              </div>
              <div className="space-y-3">
                {today.map((e) => <BigEventCard key={e.eventId} event={e} />)}
              </div>
            </section>
          )}

          {/* UPCOMING */}
          {upcoming.length > 0 && (
            <section>
              <h3 className="mb-3 text-xs font-bold uppercase tracking-wider text-on-surface-variant">Upcoming</h3>
              <div className="space-y-2">
                {upcoming.map((e) => <UpcomingEventCard key={e.eventId} event={e} />)}
              </div>
            </section>
          )}
        </>
      )}

      {/* FAB */}
      {isManager && (
        <button
          aria-label="Add event"
          className="fixed bottom-24 right-5 z-30 h-14 w-14 grid place-items-center rounded-2xl bg-grad-primary text-on-primary shadow-glow hover:opacity-95 transition active:scale-95"
        >
          <Plus className="h-6 w-6" />
        </button>
      )}
    </div>
  );
}

function BigEventCard({ event }: { event: EventItem }) {
  const c = categorise(event);
  const Icon = c.icon;
  const time = new Date(event.eventDate).toLocaleTimeString("en-IN", { hour: "2-digit", minute: "2-digit" });
  return (
    <article className={cn(
      "rounded-xl bg-surface-container border border-outline-variant/30 border-l-4 p-4 shadow-soft",
      c.tone === "tertiary" && "border-l-tertiary",
      c.tone === "error"    && "border-l-error",
      c.tone === "primary"  && "border-l-primary",
      c.tone === "secondary" && "border-l-secondary",
    )}>
      <div className="flex items-start justify-between gap-3 mb-3">
        <div className={cn(
          "grid h-9 w-9 place-items-center rounded-lg",
          c.tone === "tertiary"  && "bg-tertiary-container/30 text-tertiary",
          c.tone === "error"     && "bg-error-container/40    text-error",
          c.tone === "primary"   && "bg-primary/15            text-primary",
          c.tone === "secondary" && "bg-secondary-container/40 text-on-secondary-container",
        )}>
          <Icon className="h-4 w-4" />
        </div>
        <span className={cn(
          "text-[10px] font-bold uppercase tracking-wider px-2 py-1 rounded-full border",
          c.tone === "tertiary"  && "bg-tertiary-container/30  text-on-tertiary-container border-tertiary/30",
          c.tone === "error"     && "bg-error-container/30     text-error                  border-error/30",
          c.tone === "primary"   && "bg-primary/15             text-primary                border-primary/30",
          c.tone === "secondary" && "bg-secondary-container/40 text-on-secondary-container border-secondary/30",
        )}>
          {c.kind}
        </span>
      </div>
      <h3 className="font-bold text-base leading-snug">{event.title}</h3>
      {event.description && <p className="text-sm text-on-surface-variant mt-1 line-clamp-2">{event.description}</p>}
      <div className="mt-4 flex items-center gap-3 flex-wrap">
        {event.media.length > 0 && (
          <div className="flex items-center -space-x-2">
            {event.media.slice(0, 3).map((url, i) => (
              <a key={i} href={url} target="_blank" rel="noopener" className="h-7 w-7 rounded-full ring-2 ring-surface-container bg-surface-container-high grid place-items-center text-[10px]">
                <FileImage className="h-3.5 w-3.5 text-on-surface-variant" />
              </a>
            ))}
            {event.media.length > 3 && (
              <span className="h-7 w-7 rounded-full ring-2 ring-surface-container bg-primary/15 grid place-items-center text-[10px] font-bold text-primary">
                +{event.media.length - 3}
              </span>
            )}
          </div>
        )}
        <span className="text-xs text-on-surface-variant inline-flex items-center gap-1">
          <Clock className="h-3 w-3" />{time}
        </span>
        {event.location && (
          <span className="text-xs text-on-surface-variant inline-flex items-center gap-1">
            <MapPin className="h-3 w-3" />{event.location}
          </span>
        )}
      </div>
    </article>
  );
}

function UpcomingEventCard({ event }: { event: EventItem }) {
  const c = categorise(event);
  const d = new Date(event.eventDate);
  const isTomorrow = d.toDateString() === new Date(Date.now() + 86_400_000).toDateString();
  const Icon = c.icon;
  return (
    <article className="rounded-xl bg-surface-container border border-outline-variant/30 p-3 shadow-soft flex items-start gap-3">
      <div className="grid place-items-center rounded-lg bg-surface-container-high px-2 py-1.5 w-12 shrink-0">
        <span className="text-base font-bold leading-none">{d.getDate()}</span>
        <span className="text-[9px] font-bold uppercase tracking-wider text-on-surface-variant mt-0.5">
          {d.toLocaleDateString("en-IN", { month: "short" })}
        </span>
      </div>
      <div className="flex-1 min-w-0">
        <div className={cn(
          "inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wider mb-1",
          c.tone === "tertiary"  && "text-tertiary",
          c.tone === "error"     && "text-error",
          c.tone === "primary"   && "text-primary",
          c.tone === "secondary" && "text-on-secondary-container",
        )}>
          <Icon className="h-3 w-3" />
          {c.kind}
        </div>
        <p className="font-bold text-sm leading-snug truncate">{event.title}</p>
        <p className="text-xs text-on-surface-variant mt-0.5">
          {isTomorrow ? "Tomorrow" : fmtDate(event.eventDate)} · {d.toLocaleTimeString("en-IN", { hour: "2-digit", minute: "2-digit" })}
          {event.location && ` · ${event.location}`}
        </p>
      </div>
    </article>
  );
}

function EmptyState() {
  return (
    <div className="rounded-xl bg-surface-container border border-outline-variant/30 p-10 text-center">
      <Calendar className="mx-auto h-10 w-10 text-on-surface-variant mb-3" />
      <p className="text-sm">No team events scheduled.</p>
      <p className="text-xs text-on-surface-variant mt-1">Check back soon for upcoming milestones.</p>
    </div>
  );
}
