import { createContext, useContext, useState, useCallback, type ReactNode } from "react";
import { api, setToken, setStoredUser, clearToken, getToken, getStoredUser } from "./api";

export type Role = "Employee" | "Manager" | "Admin" | "Hr";

export interface User {
  userId: number;
  name: string;
  email: string;
  role: Role;
  teamId?: number | null;
  managerId?: number | null;
}

interface LoginResponse {
  token: string;
  user: User;
}

interface AuthState {
  user: User | null;
  token: string;
  isAuthenticated: boolean;
  isManager: boolean;
  login: (email: string, zohoToken: string) => Promise<User>;
  logout: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => getStoredUser<User>());
  const [token, setTokenState] = useState<string>(() => getToken());

  const login = useCallback(async (email: string, zohoToken: string): Promise<User> => {
    const res = await api<LoginResponse>("/auth/zoho-sso", {
      method: "POST",
      body: { email, zohoAccessToken: zohoToken },
    });
    setToken(res.token);
    setStoredUser(res.user);
    setTokenState(res.token);
    setUser(res.user);
    return res.user;
  }, []);

  const logout = useCallback(() => {
    clearToken();
    setTokenState("");
    setUser(null);
  }, []);

  const value: AuthState = {
    user,
    token,
    isAuthenticated: Boolean(token && user),
    isManager: user?.role === "Manager" || user?.role === "Admin",
    login,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}
