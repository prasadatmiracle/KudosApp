import { Award, LogOut } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth";

interface TopBarProps {
  title: string;
}

export function TopBar({ title }: TopBarProps) {
  const { user, logout } = useAuth();
  return (
    <header className="sticky top-0 z-40 bg-grad-topbar text-white shadow-glow">
      <div className="mx-auto flex max-w-3xl items-center justify-between px-4 py-3">
        <div className="flex items-center gap-2.5 min-w-0">
          <div className="grid h-8 w-8 place-items-center rounded-lg bg-white/15 backdrop-blur">
            <Award className="h-4 w-4" />
          </div>
          <div className="min-w-0">
            <h2 className="text-base font-semibold tracking-tight truncate">{title}</h2>
            {user && (
              <p className="text-[11px] text-white/70 truncate">
                {user.name} · {user.role}
              </p>
            )}
          </div>
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={logout}
          className="text-xs text-white hover:bg-white/15 hover:text-white border border-white/15"
        >
          <LogOut className="h-3.5 w-3.5" />
          <span className="hidden sm:inline">Logout</span>
        </Button>
      </div>
    </header>
  );
}
