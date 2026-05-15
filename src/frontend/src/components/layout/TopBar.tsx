import { LogOut } from "lucide-react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { ThemeToggle } from "@/components/ui/theme-toggle";
import { GenerateButton } from "@/components/layout/GenerateButton";
import { useAuth } from "@/lib/auth";
import { initials } from "@/lib/utils";

export function TopBar({ title }: { title: string }) {
  const { user, logout } = useAuth();
  return (
    <header className="fixed top-0 left-0 right-0 z-50 bg-background/80 backdrop-blur-xl border-b border-outline-variant/30">
      <div className="mx-auto flex h-14 max-w-3xl items-center justify-between px-4">
        <div className="flex min-w-0 items-center gap-2.5">
          <Avatar className="h-8 w-8 ring-2 ring-primary/30">
            <AvatarFallback className="bg-grad-primary text-xs">
              {initials(user?.name)}
            </AvatarFallback>
          </Avatar>
          <div className="min-w-0">
            <p className="text-base font-bold tracking-tight text-primary truncate leading-none">
              Kudos
            </p>
            <p className="text-[10px] text-on-surface-variant truncate mt-0.5">
              {title}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-1">
          <GenerateButton />
          <ThemeToggle />
          <button
            onClick={logout}
            aria-label="Logout"
            className="grid h-9 w-9 place-items-center rounded-full text-on-surface-variant hover:text-error hover:bg-error-container/30 transition-colors"
          >
            <LogOut className="h-4 w-4" />
          </button>
        </div>
      </div>
    </header>
  );
}
