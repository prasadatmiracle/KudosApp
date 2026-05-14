// KudosApp Service Worker — P20 PWA offline support
// Cache version: bump this string to force cache refresh on deploy
const CACHE_VERSION = "kudosapp-v1";

// Static assets to precache on install
const PRECACHE_URLS = [
  "/",
  "/index.html",
  "/app.js",
  "/app.css",
  "/icon.svg",
  "/manifest.json"
];

// API call prefix — these use network-first with short-lived cache
const API_PREFIX = "/api/";

// CDN resources — cache-first (they're versioned by URL)
const CDN_HOSTS = ["cdn.jsdelivr.net"];

// ── Install: precache static shell ─────────────────────────────────────────

self.addEventListener("install", (event) => {
  event.waitUntil(
    caches.open(CACHE_VERSION).then((cache) => cache.addAll(PRECACHE_URLS))
  );
  // Take control immediately without waiting for old SW to die
  self.skipWaiting();
});

// ── Activate: delete stale caches ──────────────────────────────────────────

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(
        keys
          .filter((key) => key !== CACHE_VERSION)
          .map((key) => caches.delete(key))
      )
    )
  );
  // Claim all open clients so the new SW controls them immediately
  self.clients.claim();
});

// ── Fetch: routing strategies ───────────────────────────────────────────────

self.addEventListener("fetch", (event) => {
  const { request } = event;
  const url = new URL(request.url);

  // Non-GET requests (POST/PUT/DELETE) go straight to network — never cache
  if (request.method !== "GET") return;

  // API calls: network-first, fall back to cache, 5 s timeout
  if (url.pathname.startsWith(API_PREFIX)) {
    event.respondWith(networkFirstWithTimeout(request, 5000));
    return;
  }

  // CDN assets: cache-first (they're content-addressed by version in URL)
  if (CDN_HOSTS.includes(url.hostname)) {
    event.respondWith(cacheFirst(request));
    return;
  }

  // App shell (HTML/JS/CSS/SVG): stale-while-revalidate
  event.respondWith(staleWhileRevalidate(request));
});

// ── Strategies ──────────────────────────────────────────────────────────────

async function networkFirstWithTimeout(request, timeoutMs) {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetch(request, { signal: controller.signal });
    clearTimeout(timer);
    if (response.ok) {
      const cache = await caches.open(CACHE_VERSION);
      cache.put(request, response.clone());
    }
    return response;
  } catch {
    clearTimeout(timer);
    const cached = await caches.match(request);
    return cached ?? offlineApiResponse();
  }
}

async function cacheFirst(request) {
  const cached = await caches.match(request);
  if (cached) return cached;
  try {
    const response = await fetch(request);
    if (response.ok) {
      const cache = await caches.open(CACHE_VERSION);
      cache.put(request, response.clone());
    }
    return response;
  } catch {
    return offlineApiResponse();
  }
}

async function staleWhileRevalidate(request) {
  const cache = await caches.open(CACHE_VERSION);
  const cached = await cache.match(request);

  // Kick off a background revalidation regardless
  const networkFetch = fetch(request)
    .then((response) => {
      if (response.ok) cache.put(request, response.clone());
      return response;
    })
    .catch(() => null);

  // Return cached immediately if available, else wait for network
  if (cached) return cached;
  const fresh = await networkFetch;
  return fresh ?? offlineShellResponse();
}

// ── Offline fallbacks ───────────────────────────────────────────────────────

function offlineApiResponse() {
  return new Response(
    JSON.stringify({ error: "You are offline. Please reconnect and try again." }),
    {
      status: 503,
      headers: { "Content-Type": "application/json" }
    }
  );
}

function offlineShellResponse() {
  return new Response(
    `<!doctype html>
<html lang="en">
<head>
  <meta charset="UTF-8"/>
  <meta name="viewport" content="width=device-width,initial-scale=1"/>
  <title>KudosApp — Offline</title>
  <style>
    body { font-family: Segoe UI, sans-serif; background:#eef4fb; color:#143049;
           display:grid; place-items:center; min-height:100vh; margin:0; }
    .card { background:#fff; border-radius:12px; padding:32px 24px;
            box-shadow:0 2px 8px rgba(8,44,75,.1); text-align:center; max-width:340px; }
    h2 { margin:0 0 8px; color:#17324a; }
    p  { color:#51697f; margin:0 0 20px; }
    button { border:none; border-radius:8px; padding:10px 20px; background:#1e6ea7;
             color:#fff; font:inherit; cursor:pointer; }
  </style>
</head>
<body>
  <div class="card">
    <h2>You're offline</h2>
    <p>KudosApp needs a connection for live data. Your last-viewed data may still be available.</p>
    <button onclick="location.reload()">Try again</button>
  </div>
</body>
</html>`,
    {
      status: 200,
      headers: { "Content-Type": "text/html; charset=utf-8" }
    }
  );
}
