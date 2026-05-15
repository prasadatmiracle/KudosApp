import { createContext, useContext, useEffect, useState, type ReactNode } from "react";

type Theme = "light" | "dark" | "system";

interface ThemeCtx {
  theme: Theme;
  resolved: "light" | "dark";
  setTheme: (t: Theme) => void;
  toggle: () => void;
}

const Ctx = createContext<ThemeCtx | null>(null);
const STORAGE_KEY = "kudos.theme";

function resolve(theme: Theme): "light" | "dark" {
  if (theme === "system") {
    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  }
  return theme;
}

export function ThemeProvider({ children, defaultTheme = "dark" }: { children: ReactNode; defaultTheme?: Theme }) {
  const [theme, setThemeState] = useState<Theme>(() => {
    const stored = (typeof localStorage !== "undefined" && localStorage.getItem(STORAGE_KEY)) as Theme | null;
    return stored ?? defaultTheme;
  });
  const [resolved, setResolved] = useState<"light" | "dark">(() => resolve(theme));

  useEffect(() => {
    const apply = () => {
      const next = resolve(theme);
      setResolved(next);
      const root = document.documentElement;
      root.classList.remove("light", "dark");
      root.classList.add(next);
      // Enable smooth transitions after first paint
      requestAnimationFrame(() => root.classList.add("theme-ready"));
    };
    apply();
    if (theme === "system") {
      const mq = window.matchMedia("(prefers-color-scheme: dark)");
      mq.addEventListener("change", apply);
      return () => mq.removeEventListener("change", apply);
    }
  }, [theme]);

  function setTheme(t: Theme) {
    localStorage.setItem(STORAGE_KEY, t);
    setThemeState(t);
  }
  function toggle() {
    setTheme(resolved === "dark" ? "light" : "dark");
  }

  return <Ctx.Provider value={{ theme, resolved, setTheme, toggle }}>{children}</Ctx.Provider>;
}

export function useTheme() {
  const v = useContext(Ctx);
  if (!v) throw new Error("useTheme must be used inside <ThemeProvider>");
  return v;
}
