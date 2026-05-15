/** Tiny JWT-aware fetch wrapper that talks to the .NET backend via Vite proxy (/api). */

const TOKEN_KEY = "kudos.token";
const USER_KEY = "kudos.user";

export function getToken(): string {
  return localStorage.getItem(TOKEN_KEY) || "";
}
export function setToken(t: string) { localStorage.setItem(TOKEN_KEY, t); }
export function clearToken() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

export function getStoredUser<T = unknown>(): T | null {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try { return JSON.parse(raw) as T; } catch { return null; }
}
export function setStoredUser(user: unknown) {
  localStorage.setItem(USER_KEY, JSON.stringify(user));
}

export class ApiError extends Error {
  status: number;
  constructor(message: string, status: number) { super(message); this.status = status; }
}

export interface ApiOptions<TBody = unknown> {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: TBody;
  signal?: AbortSignal;
}

export async function api<T = unknown>(path: string, opts: ApiOptions = {}): Promise<T> {
  const token = getToken();
  const res = await fetch(`/api${path}`, {
    method: opts.method ?? "GET",
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: opts.body !== undefined ? JSON.stringify(opts.body) : undefined,
    signal: opts.signal,
  });
  if (res.status === 401) {
    clearToken();
    if (typeof window !== "undefined") window.location.href = "/";
    throw new ApiError("Session expired", 401);
  }
  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new ApiError(text || `Request failed (${res.status})`, res.status);
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export async function apiUpload<T = unknown>(path: string, file: File): Promise<T> {
  const token = getToken();
  const fd = new FormData();
  fd.append("file", file);
  const res = await fetch(`/api${path}`, {
    method: "POST",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    body: fd,
  });
  if (res.status === 401) { clearToken(); window.location.href = "/"; throw new ApiError("Session expired", 401); }
  if (!res.ok) { const t = await res.text().catch(() => ""); throw new ApiError(t || `Upload failed`, res.status); }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}
