# UX Flow Analysis — Assessment 2

> Generated: 2026-05-15
> Based on: Full codebase re-read (all controllers, services, models, frontend pages)
> Purpose: Evaluate whether the current app flows are user-friendly and identify gaps.

---

## Flows That Work Well

### Daily Update Submission — Good
The form is simple (project, ticket, status, description). The `FocusDay` and `Continuing` statuses remove the need to fill in ticket details for common scenarios. The offline draft and streak indicator give it a habit-forming quality. This is the best-designed flow in the app.

### Validation Queue — Good
Bulk approve/reject with a single decision is genuinely efficient. The enriched detail endpoint (submitter name, proof URL, peer endorsements) means managers don't have to navigate away to make a decision.

### Activity Feed — Good
Simple, paginated, combines achievements and events. Peer endorsements making achievements visible immediately is a meaningful improvement over the pure approval-gate model.

---

## Flows With Friction

### 1. Report Generation Is a 4-Step Manual Process
**Flow:** Generate → Edit notes → Lock → Export

There is no "generate and lock in one step" option. For a weekly report that auto-generates on Friday, the manager still has to manually lock it before HR can see it. If they forget, HR sees nothing.

**Suggestion:** Add an auto-lock option after N days if the manager hasn't edited the notes. Or add a "Generate and Lock" shortcut for managers who don't need to add notes.

---

### 2. Inbox Tasks Have Too Many States for a Mobile User
**Flow:** PendingConfirmation → Active → InProgress → Completed/Dismissed

That is 4 states plus a confirm step. On mobile, that is a lot of taps. Most users want: "I see this task, I'll do it" (one tap to activate) or "not relevant" (one tap to dismiss). The category and priority picker on confirmation adds more friction.

**Suggestion:** Make category and priority optional on confirmation — default to `FollowUp` and `Medium` so users can confirm in one tap and edit later.

---

### 3. Achievement Proof Upload Is Disconnected from Submission
**Flow:** Submit achievement form → get ID back → separately upload proof file

The achievement form accepts a `ProofWorkDriveUrl` as a text field, but the actual file upload is a separate endpoint (`POST /achievements/{id}/proof/upload`). On mobile this is awkward — two separate actions for what feels like one.

**Suggestion:** The UI should handle this as a single form with an optional file picker that uploads in the background after submission, then patches the achievement record automatically.

---

### 4. No Way for Employees to See Their Own Validation Status
An employee submits an achievement and then nothing. They cannot see it in the feed (it is pending), and there is no "my submissions" view. The only feedback is the peer endorsement count if colleagues endorse it.

Requirement 6 AC 11 now addresses this (pending achievements visible to submitter in the feed), but the UI needs a clear "My Achievements" tab showing pending/approved/rejected with status and days pending.

---

### 5. Action Items Have No Self-Service ETA Update
An assignee can update status (Open → InProgress → Completed) but cannot update the due date. If a task is going to take longer than expected, the only option is to mark it InProgress and wait for the overdue escalation.

**Suggestion:** Allow assignees to propose a new due date (which notifies the creator). This is the snooze/ETA update from Assessment-1 and should be added as a requirement.

---

### 6. Sales Enquiry Submitters Have No Feedback Loop
Any employee can create a sales enquiry, but only managers can list them. An employee who submits an enquiry has no way to see it after submission — no confirmation, no status tracking. This is a dead end for the submitter.

**Suggestion:** Add `GET /sales/enquiries/my` for employees to see their own submissions with current validation status.

---

### 7. Meetings Have No List Endpoint
You can create a meeting and upload a MOM, but there is no `GET /meetings` endpoint. There is no way to browse past meetings or find a meeting to add a MOM to unless you already know the meeting ID. This is a significant usability gap.

**Suggestion:** Add `GET /meetings` (paginated, ordered by MeetingAtUtc descending) and `GET /meetings/{id}` with associated MOM entries.

---

### 8. Tasks Have No "Already Responded" Indicator
Employees can see active tasks and respond, but there is no way to see which tasks they have already responded to or what they voted. The task list shows all active tasks regardless of response status.

**Suggestion:** Add a `hasResponded` boolean and `myResponse` object to each task in the active list response, so the UI can show a checkmark or the user's current vote.

---

## Flows That Are Broken or Missing

### 1. No Logout / Token Invalidation
The frontend has a `logout()` function that clears the local token, but there is no server-side token invalidation. If a token is stolen, it remains valid for 24 hours. The requirements do not mention logout at all.

**Required addition:** `POST /auth/logout` that invalidates the token server-side (token blacklist or short-lived token with refresh token pattern).

---

### 2. No Profile Edit
There is `GET /users/me` but no `PUT /users/me`. Users cannot update their own name or notification preferences. Admins can update via CSV import but that is not a user-facing flow.

**Required addition:** `PUT /users/me` for self-service profile updates (name, notification preferences).

---

### 3. No Notification Preferences Endpoint
Requirement 19 AC 7 mentions a daily digest option, but there is no endpoint to set it. The preference needs a `PUT /users/me/preferences` endpoint and a UI toggle in the Profile screen.

---

### 4. Inbox Task Reminders Are Stubbed
The `InboxTaskReminder` entity exists and users can schedule reminders, but no background job sends them. This is a silent failure — users will schedule reminders that never arrive.

**Required addition:** A background job that checks `InboxTaskReminder` records where `IsSent = false` and `RemindAtUtc <= now`, sends the reminder via the configured channel, and marks `IsSent = true`.

---

### 5. Badge Refresh Is Manual Only
Badges are only awarded when a Manager or Admin calls `POST /performance/refresh-badges`. There is no automatic badge award when an achievement is approved or when a daily update streak hits a threshold. Users will not know they have earned a badge until someone manually triggers the refresh.

**Required addition:** Auto-trigger badge evaluation after achievement approval and at the end of each calendar month.

---

## Summary Scorecard

| Flow | Rating | Key Issue |
|------|--------|-----------|
| Daily Update submission | Good | Minor: no multi-ticket batch |
| Validation Queue | Good | — |
| Activity Feed | Good | — |
| Report generation | Friction | 4-step manual process, no auto-lock option |
| Inbox Task triage | Friction | Too many states, confirmation form too heavy on mobile |
| Achievement submission | Friction | Proof upload disconnected, no "my submissions" view |
| Action Items | Friction | No ETA update for assignees |
| Sales Enquiry | Friction | Employee cannot see own submissions after creation |
| Meetings | Broken | No list endpoint — cannot browse past meetings |
| Tasks | Friction | No "already responded" indicator in task list |
| Logout / Auth | Missing | No server-side token invalidation |
| Profile edit | Missing | No self-service profile update endpoint |
| Notification preferences | Missing | No endpoint to set preferences |
| Badge awards | Missing | Manual refresh only, no automatic trigger |

---

## Recommended Next Actions

These gaps should be addressed before the app goes to production. In priority order:

1. Add `GET /meetings` and `GET /meetings/{id}` — currently impossible to use meetings without knowing the ID
2. Add `GET /sales/enquiries/my` — employees have no feedback after submitting an enquiry
3. Add `hasResponded` + `myResponse` to task list — basic UX expectation for a voting feature
4. Add `POST /auth/logout` with server-side invalidation — security requirement
5. Add `PUT /users/me` and notification preferences endpoint — needed for the digest feature in Req 19
6. Auto-trigger badge refresh on achievement approval — silent feature otherwise
7. Add background job for Inbox Task reminders — currently a broken promise to users
8. Add action item ETA update for assignees — reduces overdue escalations and builds trust
9. Add auto-lock option for reports after N days — reduces manager overhead
10. Simplify Inbox Task confirmation to one tap with optional detail editing
