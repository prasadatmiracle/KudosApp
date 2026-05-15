import { useState, type FormEvent } from "react";
import { useNavigate, Navigate } from "react-router-dom";
import { Award, Zap, TrendingUp, Bell, Loader2 } from "lucide-react";
import { Input } from "@/components/ui/input";
import { useAuth } from "@/lib/auth";

export function Login() {
  const { login, isAuthenticated } = useAuth();
  const nav = useNavigate();
  const [email, setEmail] = useState("manager@kudos.local");
  const [token, setToken] = useState("demo-token");
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  if (isAuthenticated) return <Navigate to="/dashboard" replace />;

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setErr(null);
    setBusy(true);
    try {
      await login(email, token);
      nav("/dashboard");
    } catch (e: any) {
      setErr(e?.message || "Unable to sign in");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="min-h-dvh grid lg:grid-cols-2 bg-background">
      {/* Brand panel */}
      <div className="hidden lg:flex flex-col justify-between p-12 bg-grad-primary text-on-primary relative overflow-hidden">
        <div className="absolute inset-0 opacity-30 pointer-events-none">
          <div className="absolute top-10 right-20 h-72 w-72 rounded-full bg-white/20 blur-3xl" />
          <div className="absolute bottom-20 left-20 h-96 w-96 rounded-full bg-secondary/30 blur-3xl" />
        </div>
        <div className="relative">
          <div className="mb-14 flex items-center gap-3">
            <div className="grid h-10 w-10 place-items-center rounded-xl bg-white/15 backdrop-blur">
              <Award className="h-5 w-5" />
            </div>
            <span className="text-xl font-bold tracking-tight">Kudos</span>
          </div>
          <h2 className="mb-4 text-4xl font-bold leading-tight tracking-tight">
            Recognize.<br />Reflect.<br />Report.
          </h2>
          <p className="max-w-md text-lg text-white/85">
            Mobile-first team intelligence — turn daily updates into weekly insight automatically.
          </p>
        </div>
        <div className="relative space-y-3 text-sm">
          <Bullet icon={Zap}        text="Daily updates in under 15 seconds" />
          <Bullet icon={TrendingUp} text="Auto-generated weekly reports" />
          <Bullet icon={Bell}       text="Zoho Cliq notifications built in" />
        </div>
      </div>

      {/* Form panel */}
      <div className="flex items-center justify-center p-6">
        <form onSubmit={onSubmit} className="w-full max-w-sm space-y-5">
          <div className="lg:hidden flex items-center gap-2 mb-2">
            <div className="grid h-9 w-9 place-items-center rounded-xl bg-grad-primary text-on-primary shadow-glow">
              <Award className="h-4 w-4" />
            </div>
            <span className="text-lg font-bold tracking-tight">Kudos</span>
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight">Welcome back</h1>
            <p className="mt-1 text-sm text-on-surface-variant">Sign in with your Zoho account to continue.</p>
          </div>

          <div className="space-y-2">
            <label className="text-xs font-bold uppercase tracking-wider text-on-surface-variant">Email</label>
            <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </div>
          <div className="space-y-2">
            <label className="text-xs font-bold uppercase tracking-wider text-on-surface-variant">Zoho Access Token</label>
            <Input type="text" value={token} onChange={(e) => setToken(e.target.value)} required />
          </div>

          <button
            type="submit"
            disabled={busy}
            className="w-full h-11 rounded-lg bg-grad-primary text-on-primary font-bold text-sm shadow-glow hover:opacity-95 transition active:scale-[0.99] inline-flex items-center justify-center gap-2 disabled:opacity-60"
          >
            {busy && <Loader2 className="h-4 w-4 animate-spin" />}
            {busy ? "Signing in…" : "Sign in"}
          </button>

          {err && (
            <div className="rounded-md border-l-2 border-error bg-error-container/30 px-3 py-2 text-sm text-on-error-container">
              {err}
            </div>
          )}

          <p className="pt-2 text-center text-xs text-on-surface-variant">
            Demo: manager@kudos.local · employee@kudos.local · hr@kudos.local
          </p>
        </form>
      </div>
    </div>
  );
}

function Bullet({ icon: Icon, text }: { icon: any; text: string }) {
  return (
    <div className="flex items-center gap-3 text-white/90">
      <Icon className="h-4 w-4" />
      <span>{text}</span>
    </div>
  );
}
