import { Card, CardContent } from "@/components/ui/card";
import {
  Activity, CalendarDays, Inbox, ListChecks, PartyPopper, CheckCircle2, FileText,
} from "lucide-react";

interface StubProps {
  icon: any;
  title: string;
  message: string;
}

function Stub({ icon: Icon, title, message }: StubProps) {
  return (
    <div className="space-y-4">
      <Card className="bg-grad-mesh border-dashed">
        <CardContent className="p-10 text-center">
          <div className="mx-auto grid h-14 w-14 place-items-center rounded-2xl bg-grad-brand text-white shadow-glow">
            <Icon className="h-7 w-7" />
          </div>
          <h2 className="mt-4 text-xl font-bold tracking-tight">{title}</h2>
          <p className="mt-2 text-sm text-muted-foreground max-w-md mx-auto">{message}</p>
        </CardContent>
      </Card>
    </div>
  );
}

export function Health()      { return <Stub icon={Activity}      title="Team Health" message="Charts, blocked tickets, participation %, engagement ring — coming next iteration." />; }
export function Heatmap()     { return <Stub icon={CalendarDays}  title="Compliance Heatmap" message="GitHub-style calendar of daily submissions across your team." />; }
export function InboxPage()   { return <Stub icon={Inbox}         title="Smart Inbox" message="AI-extracted tasks from Zoho Mail and Cliq, with confirmation flow." />; }
export function Tasks()       { return <Stub icon={ListChecks}    title="Tasks & Polls" message="Active polls, action items, and quick votes." />; }
export function Events()      { return <Stub icon={PartyPopper}   title="Events" message="Team events with photo uploads to Zoho WorkDrive." />; }
export function Validation()  { return <Stub icon={CheckCircle2}  title="Validation Queue" message="Bulk approve / reject pending achievements and sales enquiries." />; }
export function Reports()     { return <Stub icon={FileText}      title="Reports" message="Weekly · Monthly · Quarterly reports with CSV / XLSX / PPTX export." />; }
