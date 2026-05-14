# KudosApp — Pending Work & Dependency Register

> **Purpose:** This file is the single source of truth for resuming development after
> any break — context loss, new session, new device. Read this + `CLAUDE.md` and you
> can pick up exactly where we left off with zero gaps.
>
> **Last updated:** 2026-05-14  
> **Completed through:** P9 (Zoho Mail, WorkDrive, Monthly auto-assembly)

---

## Quick Recap — What the App Is

Mobile-first team intelligence platform for Prasad's Microsoft Practice team at Miracle
(50+ members, Dev and QA, 2 PM–11 PM IST shift).

- **Backend:** .NET 9 Web API + EF Core + SQL Server (LocalDB in dev)
- **Frontend:** Vanilla HTML/JS served from `wwwroot/`
- **Integrations:** Zoho Cliq (bot DM + webhook), Zoho Mail, Zoho WorkDrive
- **Repo:** https://github.com/prasadatmiracle/KudosApp
- **Run:** `cd src/backend/KudosApp.Api && dotnet run`
- **Login:** `POST /api/auth/zoho-sso` body `{"email":"manager@kudos.local","zohoToken":"demo"}`

---

## Build Status

| Priority | Feature | Status |
|----------|---------|--------|
| P1 | SQL Server migration (EF Core, all controllers) | ✅ Done |
| P2 | Real Zoho Cliq (channel webhook + bot DM) | ✅ Done |
| P3 | ActionItem entity + Mon/Wed escalating reminders | ✅ Done |
| P4 | Daily update reminder @ 5 PM IST (non-submitters) | ✅ Done |
| P5 | Auto weekly report draft @ Friday 6 PM IST | ✅ Done |
| P6 | Daily compliance digest to manager @ 2 PM IST | ✅ Done |
| P7 | Zoho Mail — report email on submit/lock | ✅ Done |
| P8 | WorkDrive file upload (events + achievement proof) | ✅ Done |
| P9 | Auto monthly report assembly (last day of month) | ✅ Done |
| P9B | Smart Inbox Task Capture (Zoho Mail/Cliq → AI → tasks) | ⏳ Not started |
| P10 | Team health dashboard UI (charts) | ⏳ Not started |
| P11 | Visual compliance heatmap UI (calendar view) | ⏳ Not started |
| P12 | XLSX export (ClosedXML) | ⏳ Not started |
| P13 | PPTX export (OpenXML — 10-slide monthly deck) | ⏳ Not started |
| P14 | MOM auto-extraction (Zoho Meetings webhook + Azure OpenAI) | ⏳ Not started |
| P15 | AI weekly narrative summary | ⏳ Not started |
| P16 | Smart nudges (stale enquiries, blocked streaks) | ⏳ Not started |
| P17 | Power BI embed (leadership analytics) | ⏳ Not started |
| P18 | Real Zoho OAuth SSO (replace demo token) | ⏳ Not started |
| P19 | Azure Key Vault (move JWT + Zoho secrets out of appsettings) | ⏳ Not started |
| P20 | PWA service worker (true offline support) | ⏳ Not started |

---

## Section 1 — Pending From PRASAD (Credentials & Decisions)

These are blockers. Nothing below can go live until these are provided.

### 1A — Zoho Cliq (P2 code is ready, just needs values)

Fill these in `src/backend/KudosApp.Api/appsettings.json` under `"Zoho"`:

| Key | Where to get it | Current value |
|-----|----------------|---------------|
| `CliqWebhookUrl` | Zoho Cliq → Your Channel → Bots & Webhooks → Incoming Webhook → Copy URL | `""` (empty) |
| `CliqBotName` | Zoho Cliq → Admin Panel → Bots → your bot's name (exact, case-sensitive) | `""` (empty) |
| `CliqBotOAuthToken` | Zoho API Console → your bot → Generate Token (short-lived; P18 automates refresh) | `""` (empty) |
| `CliqApiBaseUrl` | Default `https://cliq.zoho.in/api/v2` — change to `.eu` or `.com` for your data region | pre-filled |

### 1B — Zoho Mail (P7 code is ready, just needs values)

| Key | Where to get it | Current value |
|-----|----------------|---------------|
| `MailAccountId` | Zoho Mail → Settings → Developer Space → Copy Account ID | `""` (empty) |
| `MailClientId` | Zoho API Console → your OAuth app → Client ID | `""` (empty) |
| `MailClientSecret` | Zoho API Console → your OAuth app → Client Secret | `""` (empty) |
| `MailRefreshToken` | Generate via Zoho OAuth flow (grant: `ZohoMail.messages.CREATE`) | `""` (empty) |
| `MailFromAddress` | The sender email shown to recipients (e.g. `kudosapp@miracle.com`) | `""` (empty) |
| `MailApiBaseUrl` | Default `https://mail.zoho.in/api` — change region if needed | pre-filled |

### 1C — Zoho WorkDrive (P8 code is ready, just needs values)

| Key | Where to get it | Current value |
|-----|----------------|---------------|
| `WorkDriveBaseUrl` | Default `https://www.zohoapis.in/workdrive` (or `.eu`) | `""` (empty) |
| `WorkDriveFolderId` | WorkDrive → open target folder → URL contains the folder ID | `""` (empty) |

> **Note:** WorkDrive reuses the same OAuth app as Mail. The refresh token flow is
> shared — same `MailClientId`, `MailClientSecret`, `MailRefreshToken`.

### 1D — Zoho Meetings (needed for P14)

| Key | Where to get it |
|-----|----------------|
| `MeetingsApiBaseUrl` | Zoho Meetings developer docs (e.g. `https://meeting.zoho.in/api/v2`) |
| Webhook secret | Zoho Meetings → Admin → Webhooks → Register → copy secret for signature validation |

### 1E — Azure OpenAI (needed for P14, P15)

| Item | Details needed |
|------|---------------|
| Azure OpenAI endpoint | e.g. `https://your-resource.openai.azure.com/` |
| Azure OpenAI API key | From Azure portal → your OpenAI resource → Keys |
| Deployment name | The name you gave GPT-4 when deploying (e.g. `gpt-4o`) |

### 1F — Azure Key Vault (needed for P19)

| Item | Details needed |
|------|---------------|
| Key Vault URI | e.g. `https://kudosapp-kv.vault.azure.net/` |
| App registration Client ID + Secret | For managed identity or service principal access |

### 1G — Production SQL Server (needed before go-live)

| Item | Details needed |
|------|---------------|
| Connection string | Replace `Server=(localdb)\\mssqllocaldb...` in `appsettings.json` |
| SQL login or Windows auth | Whichever the prod server uses |

> Current localdb connection is fine for development and demos.

### 1H — Decisions Needed

| # | Question | Impact |
|---|----------|--------|
| 1 | Which email addresses should receive locked report notifications? (currently all `Hr` + `Admin` role users in DB) | P7 recipients |
| 2 | WorkDrive: single shared folder for all uploads, or separate folders per category (events, achievements)? | P8 folder structure |
| 3 | P9B Smart Inbox: which Zoho Mail inbox should be monitored? (your personal inbox or a shared team mailbox?) | P9B webhook target |
| 4 | P14 MOM extraction: use Azure OpenAI (you have Azure) or Zoho Zia? | P14 AI provider |
| 5 | P17 Power BI: embed existing workspace or create new one? Do you have a Power BI Pro license? | P17 feasibility |
| 6 | P18 Zoho SSO: should all 50 team members use Zoho login, or only managers/admins? | P18 scope |

---

## Section 2 — Pending From CLAUDE (Features to Build)

These are fully specified. Ready to build as soon as prior dependencies are met
or the user says "start P__".

### P9B — Smart Inbox Task Capture
**Dependencies:** P2 ✅, P7 ✅ — ready to build  
**What it does:** Monitors Zoho Mail + Cliq DMs → AI detects action items →
confirms with user → tracks privately → optional weekly report inclusion →
notifies dependents on completion.  
**New files needed:**
- `Models/DomainModels.cs` — add `InboxTask`, `InboxTaskDependency`, `InboxTaskReminder` entities
- `Data/AppDbContext.cs` — add 3 new DbSets + Fluent API config
- `schema.sql` — add 3 new tables
- `Controllers/InboxTasksController.cs` — 9 endpoints (see CLAUDE.md for full spec)
- `Services/InboxTaskService.cs` — AI extraction, dedup (SHA-256 sender+keyword hash), state machine
- `DTOs/Contracts.cs` — add InboxTask input/output types  

**Full spec in:** `CLAUDE.md` → section "P9B — Smart Inbox Task Capture"

---

### P10 — Team Health Dashboard UI
**Dependencies:** None (API already exists at `/api/dashboard`, `/api/daily-updates/compliance-heatmap`)  
**What it does:** Rich manager dashboard page in `wwwroot/` with:
- Participation % this week (big number card)
- Blocked ticket count + owner names
- Pending validations count (achievements + sales)
- Billing type breakdown (donut chart via Chart.js CDN)
- Badge: "Team Engagement Score" (weighted average)  

**Files to change:** `wwwroot/index.html`, `wwwroot/app.js`, `wwwroot/app.css`

---

### P11 — Visual Compliance Heatmap UI
**Dependencies:** None (API exists at `GET /api/daily-updates/compliance-heatmap`)  
**What it does:** GitHub-style contribution calendar showing daily update
submission per team member. Color intensity = participation %. Click cell → see who missed.  
**Files to change:** `wwwroot/` frontend only

---

### P12 — XLSX Export
**Dependencies:** None  
**What it does:** Replace current CSV export with proper Excel files.
Sections: resource list, achievements, sales pipeline, weekly ticket log.  
**Package to add:** `ClosedXML` NuGet  
**Files to change:** `Services/DomainServices.cs` → `ReportService.Export()`,
`Controllers/ReportsController.cs`, `KudosApp.Api.csproj`

---

### P13 — PPTX Export
**Dependencies:** P12 recommended first  
**What it does:** 10-slide monthly report as PowerPoint deck.
Slides: cover, resource utilization, achievements, sales pipeline, events,
meetings/MOM, KPIs, team performance, action items, next month goals.  
**Package to add:** `DocumentFormat.OpenXml` NuGet  
**Files to change:** Same as P12 + new `Services/PptxExportService.cs`

---

### P14 — MOM Auto-Extraction from Zoho Meetings
**Dependencies:** Zoho Meetings webhook secret (1D above) + Azure OpenAI (1E above)  
**What it does:** Webhook fires when a Zoho Meeting ends → POST to
`/api/meetings/{id}/transcript-ingest` → Azure OpenAI GPT-4 extracts summary +
action items → saves to `MomEntry` → auto-creates `ActionItem` records for each
extracted action item.  
**Stub already exists:** `ZohoBridge.IngestMeetingTranscriptAsync()` — just replace the stub body  
**Files to change:** `Services/SecurityServices.cs` (replace stub),
`Controllers/MeetingsController.cs` (wire webhook validation),
`appsettings.json` (add `AzureOpenAi` section)

---

### P15 — AI Weekly Narrative Summary
**Dependencies:** Azure OpenAI (1E above), P12/P13 recommended  
**What it does:** After weekly report is generated, calls GPT-4 to produce a
2-paragraph human-readable narrative: "Your team completed X tickets this week,
Y certifications achieved, Z% participation — up/down from last week."  
Stored in `WeeklyReportPayload.AiNarrative` (new field).  
**Files to change:** `Services/DomainServices.cs` → `GenerateWeekly()`,
`DTOs/Contracts.cs`, `Models/DomainModels.cs`

---

### P16 — Smart Nudges
**Dependencies:** None  
**What it does:** Background job (daily, 2 PM IST) checks:
- Sales enquiries untouched > 7 days → Cliq DM to owner
- Employees with blocked status > 3 consecutive days → Cliq DM to manager
- Achievements pending validation > 5 days → Cliq DM to manager  
**Files to change:** `Services/ScheduledServices.cs` (new service + hosted service),
`Program.cs` (DI registration)

---

### P17 — Power BI Embed
**Dependencies:** Decision 5 from Section 1H above  
**What it does:** Embed a Power BI report in manager dashboard using
Power BI Embedded iframe. Connect SQL Server to Power BI workspace.  
**Files to change:** `wwwroot/` (embed iframe), possibly a backend token
endpoint if using service principal embed.

---

### P18 — Real Zoho OAuth SSO
**Dependencies:** Zoho OAuth app credentials (already have Client ID/Secret from P7 — reuse)  
**What it does:** Replace demo `zohoToken:"demo"` flow in `AuthController` with
real Zoho OAuth2 authorization code flow. User clicks "Login with Zoho" →
redirect → callback → exchange code for token → validate email → issue JWT.  
**Stub to replace:** `ZohoBridge.ValidateSsoAsync()` (currently always returns true)  
**Files to change:** `Services/SecurityServices.cs`, `Controllers/AuthController.cs`,
`wwwroot/index.html` (login button → Zoho redirect)

---

### P19 — Azure Key Vault
**Dependencies:** Key Vault URI + app registration (1F above)  
**What it does:** Move JWT key, all Zoho secrets out of `appsettings.json` into
Azure Key Vault. Uses `Azure.Extensions.AspNetCore.Configuration.Secrets` NuGet.  
**Files to change:** `Program.cs` (add Key Vault config provider),
`KudosApp.Api.csproj` (add NuGet), `appsettings.json` (remove secrets, add vault URI)

---

### P20 — PWA Service Worker
**Dependencies:** None  
**What it does:** Add `service-worker.js` to `wwwroot/` — caches app shell +
daily update form. Allows offline draft entry (saves to localStorage, syncs
on reconnect). Add `manifest.json` for "Add to Home Screen" on mobile.  
**Files to change:** `wwwroot/` only (no backend changes)

---

## Section 3 — Known Configuration Gaps (appsettings.json)

These keys exist in `appsettings.json` but are empty strings.
The app starts and runs without them — features degrade gracefully with a log warning.
Fill them in as you get Zoho access approved:

```
Zoho:CliqWebhookUrl          → GET FROM: Zoho Cliq channel settings
Zoho:CliqBotName             → GET FROM: Zoho Cliq admin panel
Zoho:CliqBotOAuthToken       → GET FROM: Zoho API Console
Zoho:MailAccountId           → GET FROM: Zoho Mail developer settings
Zoho:MailClientId            → GET FROM: Zoho API Console (OAuth app)
Zoho:MailClientSecret        → GET FROM: Zoho API Console (OAuth app)
Zoho:MailRefreshToken        → GENERATE: Zoho OAuth playground
Zoho:MailFromAddress         → DECIDE: your sender email
Zoho:WorkDriveBaseUrl        → GET FROM: Zoho WorkDrive API docs
Zoho:WorkDriveFolderId       → GET FROM: WorkDrive folder URL
Zoho:MeetingsApiBaseUrl      → GET FROM: Zoho Meetings developer docs
```

**Production only (do not commit):**
```
ConnectionStrings:DefaultConnection  → replace localdb with prod SQL Server
Jwt:Key                              → replace dev key with 64-char random string
```

---

## Section 4 — How to Resume in a New Session

1. Open repo: `git clone https://github.com/prasadatmiracle/KudosApp.git`
2. Read `CLAUDE.md` (architecture, all controllers, design decisions)
3. Read this file (`PENDING.md`) — tells you exactly what is done and what is next
4. Run `dotnet build` in `src/backend/KudosApp.Api` — should be 0 errors
5. Run `dotnet run` — app starts at `https://localhost:5001`
6. Tell Claude: "Continue from P__ — here are the Zoho credentials: ..."

The next items to build in order are: **P9B → P10 → P11 → P12 → P13 → P16 → P18 → P14 → P15 → P17 → P19 → P20**
(ordered by value vs complexity — P9B and P10/P11 first since they complete the core loop)

---

## Section 5 — Files to Never Lose

| File | Why it matters |
|------|---------------|
| `CLAUDE.md` | Architecture, all design decisions, P9B full spec |
| `PENDING.md` | This file — dependency register + resume guide |
| `src/backend/schema.sql` | Production SQL Server schema |
| `src/backend/KudosApp.Api/appsettings.json` | All config keys (fill in secrets) |
| `KudosApp-Pitch.html` | Manager approval deck for Zoho access |

