import { useState, type FormEvent, useRef } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Trophy, Upload, Loader2, ExternalLink } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { api, apiUpload } from "@/lib/api";
import { fmtDate } from "@/lib/utils";
import { toast } from "sonner";

interface Achievement {
  achievementId: number;
  category: string;
  title: string;
  description: string;
  validationStatus: string;
  createdAtUtc: string;
  authorName?: string;
  proofWorkDriveUrl?: string;
}

const CATEGORIES = ["Certification", "POC", "Blog", "Appreciation", "OpenSource", "Speaking", "Other"] as const;

export function Achievements() {
  const qc = useQueryClient();
  const [category, setCategory] = useState<typeof CATEGORIES[number]>("Certification");
  const [title, setTitle] = useState("");
  const [description, setDesc] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);

  const { data, isLoading } = useQuery<Achievement[]>({
    queryKey: ["ach-feed"],
    queryFn: () => api<Achievement[]>("/achievements/feed?page=1&pageSize=20"),
  });

  const submit = useMutation({
    mutationFn: async () => {
      const created = await api<{ achievementId: number }>("/achievements", {
        method: "POST",
        body: { category, title, description },
      });
      if (file && created.achievementId) {
        await apiUpload(`/achievements/${created.achievementId}/proof/upload`, file);
      }
      return created;
    },
    onSuccess: () => {
      toast.success("Achievement submitted for validation");
      setTitle(""); setDesc(""); setFile(null);
      if (fileRef.current) fileRef.current.value = "";
      qc.invalidateQueries({ queryKey: ["ach-feed"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
    },
    onError: (e: any) => toast.error(e?.message ?? "Submission failed"),
  });

  function onSubmit(e: FormEvent) {
    e.preventDefault();
    submit.mutate();
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Post an achievement</CardTitle>
          <CardDescription>Goes to your manager for validation. Attach proof to speed things up.</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={onSubmit} className="space-y-4">
            <div className="grid sm:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="cat">Category</Label>
                <select
                  id="cat"
                  value={category}
                  onChange={(e) => setCategory(e.target.value as typeof CATEGORIES[number])}
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                >
                  {CATEGORIES.map((c) => <option key={c} value={c}>{c}</option>)}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="title">Title</Label>
                <Input id="title" value={title} onChange={(e) => setTitle(e.target.value)} required />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="desc">Description</Label>
              <Textarea id="desc" rows={3} value={description} onChange={(e) => setDesc(e.target.value)} required />
            </div>
            <div className="space-y-2">
              <Label>Proof (optional)</Label>
              <label className="inline-flex cursor-pointer items-center gap-2 rounded-md border border-dashed border-primary/30 bg-primary/5 px-4 py-2 text-sm font-medium text-primary hover:bg-primary/10 transition-colors">
                <Upload className="h-4 w-4" />
                <span>{file ? file.name : "Upload certificate / file"}</span>
                <input
                  ref={fileRef}
                  type="file"
                  accept=".pdf,.png,.jpg,.jpeg,.docx,.pptx"
                  onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                  className="hidden"
                />
              </label>
            </div>
            <Button type="submit" disabled={submit.isPending}>
              {submit.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              {submit.isPending ? "Submitting…" : "Submit for validation"}
            </Button>
          </form>
        </CardContent>
      </Card>

      <section>
        <h3 className="mb-3 text-xs font-bold uppercase tracking-wider text-muted-foreground">Recent submissions</h3>
        {isLoading ? (
          <div className="space-y-2"><Skeleton className="h-20 rounded-xl" /><Skeleton className="h-20 rounded-xl" /></div>
        ) : !data || data.length === 0 ? (
          <Card><CardContent className="p-8 text-center text-sm text-muted-foreground">No achievements posted yet.</CardContent></Card>
        ) : (
          <div className="space-y-2">
            {data.map((a) => (
              <Card key={a.achievementId}>
                <CardContent className="flex items-start gap-3 p-4">
                  <div className="grid h-10 w-10 place-items-center rounded-xl bg-primary/10 text-primary">
                    <Trophy className="h-5 w-5" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-2">
                      <p className="font-semibold leading-snug">{a.title}</p>
                      <Badge variant={a.validationStatus === "Approved" ? "success" : a.validationStatus === "Rejected" ? "destructive" : "warning"}>
                        {a.validationStatus}
                      </Badge>
                    </div>
                    <p className="text-sm text-muted-foreground mt-1">{a.description}</p>
                    <div className="mt-2 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                      <Badge variant="outline" className="text-[10px]">{a.category}</Badge>
                      {a.authorName && <span>{a.authorName}</span>}
                      <span>{fmtDate(a.createdAtUtc, "long")}</span>
                      {a.proofWorkDriveUrl && (
                        <a className="inline-flex items-center gap-1 text-primary hover:underline" href={a.proofWorkDriveUrl} target="_blank" rel="noopener">
                          Proof <ExternalLink className="h-3 w-3" />
                        </a>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
