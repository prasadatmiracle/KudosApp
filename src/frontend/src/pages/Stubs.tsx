import { ListChecks, PartyPopper } from "lucide-react";

function Stub({ icon: Icon, title, message }: { icon: any; title: string; message: string }) {
  return (
    <div className="rounded-xl bg-grad-mesh border border-dashed border-outline-variant/40 p-10 text-center">
      <div className="mx-auto grid h-14 w-14 place-items-center rounded-2xl bg-grad-primary text-on-primary shadow-glow">
        <Icon className="h-7 w-7" />
      </div>
      <h2 className="mt-4 text-xl font-bold tracking-tight">{title}</h2>
      <p className="mt-2 text-sm text-on-surface-variant max-w-md mx-auto">{message}</p>
    </div>
  );
}

export function Tasks()  { return <Stub icon={ListChecks}  title="Tasks & Polls" message="Active polls, action items, and quick votes — coming next iteration." />; }
export function Events() { return <Stub icon={PartyPopper} title="Events"        message="Team events with photo uploads to Zoho WorkDrive." />; }
