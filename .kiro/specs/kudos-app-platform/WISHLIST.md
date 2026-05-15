# KudosApp — Wishlist & Pending Items

> **Purpose:** Single source of truth for SCR-1 items, follow-ups, and ideas
> that have been parked rather than dropped. Anything not yet shipped lives here.
>
> **Last updated:** 2026-05-15
> **See also:** `SCR-1.md` (the full spec change request), `Assessment-1.md`
> (UX critique), `suggestions.md` (correctness gaps), `requirements.md` (spec)

---

## How to use this file

- **Status legend:** `🟢 Done` · `🟡 Partial` · `🔴 Pending` · `🧊 Icebox`
- Add new items at the bottom of the relevant section with the same row format
  so this stays grep-able.
- When you ship something, move the row to **Done in this release** at the
  bottom with the commit hash for traceability.

---

## P1 — SCR-1 backend changes (committed by spec, not yet built)

These are the items from `SCR-1.md` that touch schedulers, services, or
the data model — they are not visible until the corresponding backend
work is done. The UI tone/labels have already been reframed so that when
these ship they surface in the right framing.

| ID  | Item                                                                   | Status | Notes |
|-----|------------------------------------------------------------------------|--------|-------|
| C15 | Action items: replace Mon/Wed escalation with due-date-relative reminders (3 days out → assignee, 1 day out → assignee, post-due → manager) | 🔴 Pending | Touches `ActionItemReminderService` (BackgroundService at 8 AM UTC). Add `Snooze / Update ETA` action that bumps DueDate. |
| C21 | Notification quiet hours (no DMs before 14:00 IST or after 23:00 IST) + daily-digest opt-in (one consolidated DM instead of multiple) | 🔴 Pending | Wrap `ZohoBridge.SendCliqNotificationAsync` with a quiet-hours guard. Add `UserNotificationPreference` entity. |
| C22 | Blocked-ticket nudge — employee on day 3, manager on day 5 (currently goes straight to manager at day 3) | 🔴 Pending | Update `SmartNudgesService` blocked-ticket logic. Tone note: phrasing must be supportive ("do you need anything to unblock this?") not accusatory. |
| C23 | Achievement-pending nudge fires at 3 days instead of 5; tone shifted to "helpful reminder" not "system alert" | 🔴 Pending | Update `SmartNudgesService` achievement-pending logic. |
| C26 | Peer endorsement for achievements — 2+ peer `+1`s makes an achievement visible in the feed immediately with a `Peer Endorsed` badge, parallel to manager approval | 🔴 Pending | New `AchievementEndorsement` entity (AchievementId, UserId, CreatedAtUtc). `POST /api/achievements/{id}/endorse`. Feed must show pending-but-endorsed items + employee's own pending items only to them. |
| C24 | Smart nudge counts endpoint refactor — make it an explicit `GET /api/nudges/counts` rather than implicit server-side counter | 🟡 Partial | Endpoint exists. Verify response shape matches `{ staleEnquiries, blockedStreaks, pendingAchievements }`. |
| C25 | When confirming an Inbox_Task, offer `FocusDay` / `Continuing` as status options on the linked Daily_Update | 🔴 Pending | Wire after inbox-task-to-daily-update flow exists. |
| C27 | New Requirement 26 — Master Data and Resource Allocation endpoints | 🟡 Partial | Endpoints exist (`GET /api/master-data/projects`, `/teams`, `POST /resource-allocation`). Update `requirements.md` text per SCR. |
| C28 | Performance SLA — all write endpoints under 2000 ms for 50 concurrent users (excluding external Zoho calls) | 🔴 Pending | Needs a load-test harness (`k6` or `bombardier`). Add to CI later. |

---

## P2 — SCR-1 requirements doc updates (text-only)

These are pure documentation changes — they have no runtime effect but
need to land in `requirements.md` to keep the spec in sync with reality.

| ID  | Item                                                                   | Status |
|-----|------------------------------------------------------------------------|--------|
| C1  | Glossary: rename `Compliance_Heatmap` → `Participation_Calendar`       | 🔴 Pending (UI already renamed) |
| C2  | Req 4 AC 8 — rewrite to reference `Participation_Calendar`              | 🔴 Pending |
| C3  | Req 4 AC 9 — add range heatmap acceptance criterion                    | 🔴 Pending |
| C4  | Req 4 AC 10 — document 5-point award on `DailyUpdate` submission       | 🔴 Pending |
| C5  | Req 4 AC 2 — document new `FocusDay` + `Continuing` statuses           | 🟡 Partial (enum + UI shipped; spec text not yet rewritten) |
| C6  | Req 4 AC 11 — document FocusDay/Continuing accepting empty ticket + neutral colour in UI | 🟡 Partial (controller + UI shipped; spec text not yet rewritten) |
| C7  | Req 5 AC 5 — correct duplicate `TaskResponse` to "update existing"     | 🔴 Pending |
| C8  | Req 5 AC 9 — document 2-point award on new `TaskResponse`              | 🔴 Pending |
| C9  | Req 6 AC 7 — document 10-point award on `Achievement` creation         | 🔴 Pending |
| C10 | Req 6 AC 8 — document 10 MB upload limit returns HTTP 413              | 🔴 Pending |
| C11 | Req 7 AC 1 — correct sales enquiry creation role restriction (any user)| 🔴 Pending |
| C12 | Req 8 AC 7 — document the enriched `validation/pending-detail` endpoint | 🔴 Pending |
| C13 | Req 9 AC 9 — document the AI narrative endpoint                        | 🔴 Pending |
| C14 | Req 12 AC 3 — correct duplicate MOM constraint to "multiple allowed"   | 🔴 Pending |
| C16 | Req 14 AC 5/6 — direct WorkDrive file upload + 20 MB cap + 502 fallback | 🔴 Pending |
| C17 | Req 15 AC 3 — correct activity feed pagination claim                   | 🔴 Pending |
| C18 | Req 16 AC 3 — specify Consistent/Team Player/Knowledge Sharer thresholds explicitly | 🔴 Pending |
| C19 | Req 16 AC 6/7/8 — top-10 leaderboard scoping + personal trend          | 🟡 Partial (UI shipped; spec text not yet rewritten; backend currently returns full list with UI doing the slice — should move scoping server-side) |
| C20 | Req 18 AC 2 — correct "past 4 weeks" → "past 7 days"                  | 🔴 Pending |
| C29 | Glossary additions (`FocusDay`, `Continuing`, `Peer_Endorsement`, `Streak`) + update `Smart_Nudge` tone | 🔴 Pending |

---

## P3 — UX polish (not in SCR but worth doing)

| Item                                                                          | Status |
|-------------------------------------------------------------------------------|--------|
| **Streak indicator on the Daily check-in page** is currently stubbed to "0 Day Streak" using `mine?.length`. Compute actual consecutive-day streak from `/daily-updates/my`. | 🔴 Pending |
| **Personal points trend on Dashboard** is currently synthesised (72% / 86% / 100% of current month). Wire to a real `/performance/trend?months=3` endpoint. | 🔴 Pending |
| **Project dropdown on Daily form** is hard-coded to projectId=1. Fetch projects from `GET /api/master-data/projects` and let user pick. | 🔴 Pending |
| **Team Health "missing today"** currently shows up to 6 avatars; reveal full list in a sheet/modal when clicked | 🔴 Pending |
| **Heatmap role chips** are hard-coded to "Developer" placeholder. Backend should include each user's actual role in the response. | 🔴 Pending |
| **Reports detail page** — clicking a report row currently does nothing. Wire to a detail view with full payload + AI narrative. | 🔴 Pending |
| **Notification bell** in TopBar — there's a `Bell` icon in some Stitch designs we haven't added. Add to surface inbox/validation counts. | 🔴 Pending |
| **Dark mode toggle should default to system pref** the very first time (currently defaults to `dark`). Honour `prefers-color-scheme` for unset users. | 🔴 Pending |

---

## P3a — "Generate" button action plan (context-aware AI)

The top-right **Generate** button is now route-aware: it only renders on
pages with a wired action, and shows a context-specific label. Phase 1
ships informational toasts so the affordance is discoverable; phases 2–3
wire real backend flows.

| Page | Generate action | Status | Backend endpoint |
|------|----------------|--------|------------------|
| `/reports`      | "Generate report" — period picker + draft creation | 🟡 Stub toast | `POST /api/reports/{weekly\|monthly\|quarterly}/generate` (exists; need UI picker) |
| `/feed`         | "AI weekly summary" — narrative of last 7 days team activity | 🟡 Stub toast | Generalise `GET /api/reports/{id}/narrative` to a team-feed variant |
| `/daily`        | "Draft from inbox" — AI-fill from confirmed inbox tasks    | 🟡 Stub toast | **New:** `POST /api/daily-updates/draft-from-inbox` |
| `/validation`   | "Suggest auto-approvals" — flag low-risk items             | 🔴 Pending     | **New:** `GET /api/validations/suggestions` |
| `/achievements` | "Draft from recent work"                                   | 🔴 Pending     | **New:** `GET /api/achievements/draft-suggestions` |
| `/inbox`        | "Re-run extraction" — manual ingest refresh                | 🔴 Pending     | `POST /api/inbox-tasks/ingest/mail` |
| `/health`       | "Send team digest"                                         | 🔴 Pending     | `ZohoBridge.SendCliqNotificationAsync` |
| All others (Profile, Calendar, Events, Tasks, Leaderboard) | Hidden | 🟢 Done | n/a |

---

## P3b — Bottom nav overflow

Bottom dock previously held 13 items in a horizontal scroller (overwhelming).
Now: **4 primary tabs + More button** that opens a slide-up sheet with the
rest. Primary stays consistent across roles; role-specific items live under
More with a "Manager" pill.

| Primary tabs (always)  | Under More (employee)              | Under More (manager — adds these) |
|------------------------|------------------------------------|-----------------------------------|
| Home · Check-in · Feed · Profile | Inbox · Tasks · Kudos · Events · Top 10 · Reports | + Pulse · Calendar · Review |

Implemented in `BottomNav.tsx`. Sheet dismisses on route change, ESC, or backdrop click. Active overflow page highlights the More button itself.

---

## P4 — Stitch v2 designs not yet applied

These designs were in `stitch_kudosapp_ui_design_reference (1).zip`:

| Design | Status | Notes |
|--------|--------|-------|
| `reports_enhanced_dark` — sidebar-style filter panel + System Insights card + tinted AI Summary | 🔴 Pending | Refactor `Reports.tsx` from horizontal tab filter to sidebar layout on larger screens. |
| `inbox_tasks_light` — confirm light-mode layout parity | 🟢 Done | Existing InboxPage works in both themes (driven by CSS vars). |
| `team_events_light` — confirm light-mode parity | 🟢 Done | Events page works in both themes. |
| `daily_update_form` (light, plain — no `_dark` suffix) | 🟢 Done | Daily already covered. |
| `high_fidelity_midnight/DESIGN.md` | 🟢 Done | Confirmed identical to existing dark palette. |

---

## P5 — Production hardening (existing CLAUDE.md roadmap)

These were already in `CLAUDE.md` as P14–P20; not strictly SCR-1 but worth tracking here.

| Item                                                                          | Status |
|-------------------------------------------------------------------------------|--------|
| **P14** — MOM auto-extraction (Zoho Meetings webhook + Azure OpenAI)         | 🧊 Icebox |
| **P15** — AI weekly narrative summary (real implementation, not stub)         | 🟡 Partial |
| **P16** — Smart nudges polish (stale enquiries, blocked streaks)              | 🟡 Partial (data fetches, tone updates pending C22/C23) |
| **P17** — Power BI embed (leadership analytics)                                | 🧊 Icebox |
| **P18** — Real Zoho OAuth SSO (replace demo token)                            | 🔴 Pending |
| **P19** — Azure Key Vault (JWT + Zoho secrets)                                | 🔴 Pending |
| **P20** — PWA service worker (true offline support)                           | 🟡 Partial (Vite build hasn't included SW yet) |

---

## Done in this release (2026-05-15)

| ID    | Item                                                                  | Commit |
|-------|----------------------------------------------------------------------|--------|
| —     | Full Vite + React + TypeScript + shadcn-style component frontend     | `41bcff4` |
| —     | Material 3 light + dark theme tokens from Stitch DESIGN.md files     | `796f4de` |
| —     | Pages built: Login, Dashboard, Daily, Feed, Profile, Leaderboard, Achievements, Health, Heatmap, Inbox, Validation, Reports | `796f4de` |
| C5/C6 | `DailyStatus.FocusDay` + `Continuing` enum values; ticket-optional submission; neutral colour on the calendar | `a8dd8a5` |
| C19   | Top-10 leaderboard for non-managers + personal "your position" card + 3-month points trend on Dashboard (UI only) | `a8dd8a5` |
| A1/A3/A5/A7 | Page titles & nav labels reframed: Daily check-in, Need help (status pill), Participation calendar, Recognitions to review, Team pulse, Top 10 | `a8dd8a5` |
| —     | New `Events` page matching `team_events_enhanced_dark` design        | `a8dd8a5` |
| —     | New `Tasks` page (per-task voting card)                              | `a8dd8a5` |
| —     | Heatmap reads `{ dates, users }` envelope; surfaces API errors      | `<this commit>` |
