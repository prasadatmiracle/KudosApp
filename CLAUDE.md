# KudosApp — Claude Code Context

## What This App Is
Mobile-first team intelligence platform for a 50-member engineering/sales team.
Converts daily inputs (work logs, votes, achievements, sales data, events) into
automated weekly/monthly/quarterly reports. Manager-owned; HR gets read-only access
to locked reports.

Owner: Prasad (Manager) — React/.NET/Azure background, Zoho ecosystem user.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 9 Web API (ASP.NET Core) |
| Persistence | **In-memory now** → SQL Server target (`schema.sql` ready) |
| Auth | JWT Bearer — Zoho SSO stub in `AuthController` |
| Frontend | Vanilla HTML/CSS/JS (no npm, no build step) — served from `wwwroot/` |
| Notifications | Zoho Cliq webhook stub (`ZohoBridge.cs`) |
| File storage | Zoho WorkDrive (URL stored only — no upload yet) |

---

## Project Layout

```
KudosApp.sln
└── src/backend/KudosApp.Api/
    ├── Controllers/          17 controllers (see list below)
    ├── Models/DomainModels.cs   19 domain entities + enums
    ├── DTOs/Contracts.cs        request/response types
    ├── Services/                business logic, data store, Zoho bridge
    ├── Infrastructure/          JWT config, UserContext, controller extensions
    ├── wwwroot/                 index.html + app.js + app.css (SPA frontend)
    ├── schema.sql               production SQL Server schema (ready to use)
    └── Program.cs               DI wiring + middleware pipeline
```

---

## Domain Models (DomainModels.cs)

19 entities: `Team`, `UserProfile`, `Project`, `ResourceAllocation`, `DailyUpdate`,
`TaskItem`, `TaskResponse`, `Achievement`, `SalesEnquiry`, `Engagement`,
`SalesSession`, `EventItem`, `EventMedia`, `MeetingRecord`, `MomEntry`,
`ValidationRecord`, `PointsLog`, `Badge`, `UserBadge`, `ReportRecord`,
`AuditEntry`, `ReminderDispatch`

Key enums: `AppRole` (Employee/Manager/Admin/Hr), `BillingType`
(Billable/NonBillable/Shadow/Trainee/Overhead), `TaskType` (Vote/Action/Info),
`DailyStatus` (Open/InProgress/Completed/Blocked/NoTask), `ValidationStatus`
(Pending/Approved/Rejected), `ReportType` (Weekly/Monthly/Quarterly),
`ReportStatus` (Draft/Finalized/Locked)

---

## Controllers

| Controller | Key Routes | Auth |
|-----------|-----------|------|
| AuthController | POST /api/auth/zoho-sso | Public |
| DashboardController | GET /api/dashboard | Any |
| DailyUpdatesController | POST /api/daily-updates, GET /api/daily-updates/team, GET compliance-heatmap | Manager+ for team/heatmap |
| TasksController | POST /api/tasks, GET /api/tasks/active, POST /{id}/respond, GET /{id}/report | Manager+ to create |
| AchievementsController | POST /api/achievements, GET /api/achievements/feed | Any |
| SalesController | POST enquiries/engagements/sessions, GET /api/sales/enquiries | Manager+ for list |
| EventsController | POST /api/events, POST /{id}/media (max 10), GET /api/events/feed | Any |
| MeetingsController | POST /api/meetings, POST /{id}/mom, POST /{id}/transcript-ingest | Any |
| ValidationsController | GET /api/validations/pending, POST /{id}/decision, POST /bulk | Manager+ |
| ReportsController | POST weekly/monthly/quarterly/generate, PUT weekly/{id}, POST /{id}/submit, GET /{id}/export | Manager/Admin/Hr |
| PerformanceController | GET leaderboard, GET my, POST refresh-badges | Manager+ for refresh |
| AuditController | GET /api/audit | Admin only |
| FeedController | GET /api/feed | Any |
| NotificationsController | POST /api/notifications/send (stub) | Manager+ |
| MasterDataController | POST bulk import CSV | Admin |
| UsersController | GET /api/users/me, GET /api/users | Any |
| HealthController | GET /health | Public |

---

## Services

| Service | File | Notes |
|---------|------|-------|
| InMemoryDataStore | Services/InMemoryDataStore.cs | **Replace with EF Core (P1)** |
| DataSeeder | Services/DataSeeder.cs | Seeds test users/projects/badges on startup |
| TokenService | Services/TokenService.cs | Generates JWT with role/team claims |
| ZohoBridge | Services/ZohoBridge.cs | **Stub** — wire real Cliq/Mail/WorkDrive (P2, P7, P8) |
| AuditService | Services/AuditService.cs | Immutable audit log |
| ReminderPolicy | Services/ReminderPolicy.cs | Caps 2 reminders/user/day |
| VisibilityService | Services/VisibilityService.cs | Role-based data visibility chain |
| PointsService | Services/PointsService.cs | Awards: DailyUpdate+5, Vote+2, Achievement+10 |
| ReportService | Services/ReportService.cs | Weekly/monthly/quarterly aggregation + CSV export |

---

## Seeded Test Users (dev only)

| Email | Role | Password (demo) |
|-------|------|----------------|
| admin@kudos.local | Admin | any token via /api/auth/zoho-sso |
| manager@kudos.local | Manager | any token via /api/auth/zoho-sso |
| employee@kudos.local | Employee | any token via /api/auth/zoho-sso |
| hr@kudos.local | Hr | any token via /api/auth/zoho-sso |

Login: `POST /api/auth/zoho-sso` with `{"email":"manager@kudos.local","zohoToken":"demo"}`

---

## How to Run

```bash
cd src/backend/KudosApp.Api
dotnet run
# App: https://localhost:5001
# Swagger: https://localhost:5001/swagger
# Frontend: https://localhost:5001 (served from wwwroot/)
```

---

## P1–P20 Build Roadmap

### COMPLETED
- [x] **P1** — SQL Server migration (AppDbContext, all 16 controllers + all services migrated, build clean)
- [x] **P2** — Real Zoho Cliq integration (channel webhook + per-user bot DM, IHttpClientFactory, ZohoOptions, graceful degradation)
- [x] **P3** — ActionItem entity + escalating reminders (Monday assignee DM, Wednesday manager DM, BackgroundService at 8 AM UTC, manual trigger endpoint)

### IN PROGRESS
- (none — P3 done, ready for P4)

### QUEUE

#### Core Automation
- [ ] P4 — Daily update reminder scheduler (5 PM, non-submitters via Cliq)
- [ ] P5 — Auto weekly report generation (Friday 6 PM, pre-populated draft)
- [ ] P6 — Daily compliance digest to manager (10 AM: participation %, blocked, pending)

#### Core Automation
- [ ] P3 — Action items entity + escalating reminders (Monday → assignee, Wednesday → manager)
- [ ] P4 — Daily update reminder scheduler (5 PM, non-submitters via Cliq)
- [ ] P5 — Auto weekly report generation (Friday 6 PM, pre-populated draft)
- [ ] P6 — Daily compliance digest to manager (10 AM: participation %, blocked, pending)

#### Zoho Integrations
- [ ] P7 — Zoho Mail (report email distribution on submit)
- [ ] P8 — WorkDrive file upload (events + achievement proof)
- [ ] P9 — Auto monthly report assembly (last day of month → email)
- [ ] P9B — Smart Inbox Task Capture (requires P2 + P7; see full spec below)

#### Manager Dashboard
- [ ] P10 — Team health dashboard (charts: participation, billing type, engagement score)
- [ ] P11 — Visual compliance heatmap UI (GitHub-style calendar, API already exists)
- [ ] P12 — XLSX export (ClosedXML — resource list, achievements, sales pipeline)
- [ ] P13 — PPTX export (OpenXML — 10-slide monthly deck)

#### AI & Intelligence
- [ ] P14 — MOM auto-extraction (Zoho Meetings webhook + Azure OpenAI)
- [ ] P15 — AI weekly narrative summary
- [ ] P16 — Smart nudges (stale enquiries, blocked streaks)
- [ ] P17 — Power BI embed (leadership analytics)

#### Production Hardening
- [ ] P18 — Real Zoho OAuth SSO (replace demo token)
- [ ] P19 — Azure Key Vault (move JWT + Zoho secrets)
- [ ] P20 — PWA service worker (true offline support)

---

## P1 — DONE (SQL Server Migration)

All 16 controllers + all services migrated to AppDbContext. `dotnet build` passes 0 errors.
Connection string targets `(localdb)\mssqllocaldb;Database=KudosAppDb`.
Run `dotnet run` — EnsureCreated auto-creates schema and seeds test users.

---

## P9B — Smart Inbox Task Capture (spec)

### Dependencies
Must complete P2 (real Zoho Cliq) and P7 (Zoho Mail) first.

### What it does
Monitors Zoho Mail inbox + Zoho Cliq DMs → AI detects tasks/questions/action items
directed at the user → confirmation flow → private task tracking → optional weekly
report inclusion → optional public visibility with dependency notifications.

### Privacy rules (LOCKED)
- **Private (default):** visible to task owner + their direct manager only
- **Public:** visible to owner + manager + tagged dependents + all team peers
- Manager sees ALL tasks for their reports regardless of private flag

### Confirmed flow
```
[Zoho Mail webhook / Cliq event]
  → POST /api/inbox-tasks/ingest/mail  (or /ingest/cliq)
  → AI extracts task text + sender
  → Dedup check: sender + keyword-similarity hash (24h window)
  → If new: create InboxTask (State=PendingConfirmation)
  → Notify user: Cliq DM + in-app notification bell
  → User opens app → sees pending card
  → Confirm: pick Category + Priority + DueDate → State=Active
    OR Dismiss → State=Dismissed (no re-alert)
  → If public: tag dependents → each gets Cliq DM notification
  → User works task: Active → InProgress → Completed
  → On Complete: prompt "Add to this week's report?"
      → Yes → pick WeeklyReportCategory (RoutineTask/Accomplishment/Achievement/Other)
      → No → stays private, not in report
  → If public + has dependents: Cliq DM to each dependent "Task done"
  → Weekly report GenerateWeekly picks up completed InboxTasks by category bucket
```

### New entities needed
```
InboxTask
  InboxTaskId          int PK IDENTITY
  UserId               int FK → Users
  SourceChannel        nvarchar(20)   -- 'ZohoMail' | 'ZohoCliq'
  SourceSender         nvarchar(200)  -- email or Cliq user ID
  SourceMessageId      nvarchar(500)  -- for raw dedup
  SourcePreview        nvarchar(1000) -- first 500 chars of message
  ExtractedTaskText    nvarchar(2000) -- AI output
  DeduplicationHash    nvarchar(64)   -- sha256(sender + normalised keywords)
  IsPrivate            bit DEFAULT 1
  Category             nvarchar(50)   -- enum: Development|FollowUp|StatusUpdate|
                                      --       ReportGeneration|Support|Communicate|Custom
  CustomCategoryName   nvarchar(100)  -- only when Category='Custom'
  Priority             nvarchar(20)   -- Low|Medium|High|Critical
  State                nvarchar(30)   -- PendingConfirmation|Active|InProgress|
                                      --                    Completed|Dismissed
  DueAtUtc             datetime2 NULL
  CompletedAtUtc       datetime2 NULL
  IncludeInWeeklyReport bit DEFAULT 0
  WeeklyReportCategory nvarchar(30) NULL  -- RoutineTask|Accomplishment|Achievement|Other
  CreatedAtUtc         datetime2 DEFAULT GETUTCDATE()
  UpdatedAtUtc         datetime2 NULL

InboxTaskDependency
  InboxTaskDependencyId  int PK IDENTITY
  InboxTaskId            int FK → InboxTasks
  DependentUserId        int FK → Users
  NotifiedCompletedAtUtc datetime2 NULL

InboxTaskReminder
  InboxTaskReminderId  int PK IDENTITY
  InboxTaskId          int FK → InboxTasks
  RemindAtUtc          datetime2
  Channel              nvarchar(20)  -- 'Cliq' | 'InApp' | 'Both'
  IsSent               bit DEFAULT 0
```

### New API endpoints
```
POST /api/inbox-tasks/ingest/mail          -- Zoho Mail webhook (internal/system)
POST /api/inbox-tasks/ingest/cliq          -- Zoho Cliq event (internal/system)
GET  /api/inbox-tasks/pending              -- list PendingConfirmation for current user
GET  /api/inbox-tasks                      -- list Active/InProgress for current user
POST /api/inbox-tasks/{id}/confirm         -- confirm + set category/priority/due
POST /api/inbox-tasks/{id}/dismiss         -- dismiss
PUT  /api/inbox-tasks/{id}/state           -- update state (Active→InProgress→Completed)
POST /api/inbox-tasks/{id}/complete        -- mark done + choose weekly report category
POST /api/inbox-tasks/{id}/make-public     -- promote + tag dependents
GET  /api/inbox-tasks/team                 -- manager view: all tasks for visible team
```

### AI extraction prompt (to Zoho Zia / Azure OpenAI)
```
"Given this email/chat message, does it contain a task, question, or action item
directed at the recipient? If yes, extract: (1) a concise task description in one
sentence, (2) any mentioned deadline. Respond as JSON:
{ "hasTask": bool, "taskText": string|null, "deadline": string|null }"
```

### Dedup algorithm
```csharp
string ComputeHash(string sender, string taskText)
{
    var normalised = Regex.Replace(taskText.ToLowerInvariant(), @"\s+", " ").Trim();
    var keywords = string.Join(" ", normalised.Split(' ')
        .Where(w => w.Length > 4)
        .OrderBy(w => w)
        .Take(8));
    return SHA256.HashData(Encoding.UTF8.GetBytes($"{sender}|{keywords}"));
}
// Reject if hash exists in InboxTasks where CreatedAtUtc > now-24h
```

### Weekly report integration
ReportService.GenerateWeekly adds a new section `InboxTaskSummary`:
- Group completed InboxTasks (current user, current week) by WeeklyReportCategory
- Each group renders as its own bullet list under the matching section heading

---

## Key Design Decisions (Don't Change Without Reason)

- **Enums as strings in JSON** — `JsonStringEnumConverter` registered globally in Program.cs
- **DateOnly for dates** — WorkDate, StartDate, SessionDate, EventDate use `DateOnly` not `DateTime`
- **2-reminder daily cap** — enforced by `ReminderPolicy.cs`, backed by `ReminderDispatch` table
- **Manager visibility chain** — employee → direct manager → skip-level → admin (VisibilityService.cs)
- **Report lock flow** — Draft → Finalized (manager submits) → Locked (admin locks); HR sees Locked only
- **Max 10 media files per event** — enforced in EventsController
- **JWT claims** — user_id, name, email, role, team_id, manager_id

---

## Configuration (appsettings.json)

```json
{
  "Jwt": {
    "Key": "ReplaceThisWithASecureKeyForDevOnly123456789",
    "Issuer": "KudosApp",
    "Audience": "KudosUsers",
    "ExpiryMinutes": 120
  }
}
```

CORS allows: `http://localhost:5173` and `http://127.0.0.1:5173`

---

## What NOT to Do

- Do not add `[ApiController]` base route changes — all routes are explicitly declared
- Do not switch to EF Core lazy loading — use explicit `.Include()` only
- Do not add password hashing yet — P18 covers real auth; dev uses demo tokens
- Do not remove `InMemoryDataStore` until EF Core services pass all existing API tests
