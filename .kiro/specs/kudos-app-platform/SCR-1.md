# Spec Change Request — SCR-1

> **Document:** `.kiro/specs/kudos-app-platform/requirements.md`  
> **Prepared:** 2026-05-15  
> **Sources:** suggestions.md (S1–S17), Assessment-1.md (A1–A7)  
> **Instruction:** Apply every change listed below to `requirements.md` exactly as specified. Do not rewrite sections not mentioned. Do not change numbering of unchanged items.

## Implementation status (UI-facing changes from this SCR)

The following SCR items have been applied to the React frontend
(`src/frontend/`) and backend models, ahead of the formal `requirements.md`
rewrite. Items not listed below remain to be applied to `requirements.md`
when the spec doc is updated.

| Item   | Where implemented                                                                                |
|--------|--------------------------------------------------------------------------------------------------|
| C1/C2  | Heatmap renamed "Participation calendar" in `AppShell.tsx` titles and `BottomNav.tsx` labels     |
| C5/C6  | `DailyStatus` enum gained `FocusDay` + `Continuing`; UI shows them as pill options in `Daily.tsx`; Heatmap renders them in a neutral tone (`Heatmap.tsx`) |
| C19    | Leaderboard returns top-10 visible + personal "your position" card for non-managers (`Leaderboard.tsx`); managers still see full list. Personal 3-month points trend added to Dashboard (`Dashboard.tsx`) |
| A5/A1  | Page titles + nav labels reworded to engagement framing (Pulse / Check-in / Top 10 / Review)     |
| A7     | "Validation Queue" page header reworded to "Recognitions to review" (`Validation.tsx`)           |
| A3     | "Blocked" daily-status pill labelled "Need help" in `Daily.tsx` (employee-first framing)         |

Backend rule changes (C15/C21/C22/C23 — schedulers, quiet hours, employee-first
nudge cascades) and data model additions (C26 peer endorsement) remain to be
implemented; the UI titles and palette are now consistent with the new tone so
that those backend changes will surface in the right framing when wired.

---

## Part 1 — Accuracy Corrections (from suggestions.md)

These fix places where the requirements contradict or omit what the code actually does.

---

### C1 — Glossary: Rename "Compliance_Heatmap"

**Location:** Glossary section  
**Action:** Replace the existing `Compliance_Heatmap` glossary entry with:

```
- **Participation_Calendar**: A calendar-style visualization of a team member's Daily_Update submission history, showing participation patterns over time.
```

---

### C2 — Req 4 AC 8: Rename heatmap reference

**Location:** Requirement 4, Acceptance Criteria 8  
**Action:** Replace `Compliance_Heatmap` with `Participation_Calendar` in the text. Replace the full AC 8 with:

```
8. WHEN a Manager or Admin requests the Participation_Calendar for a single date, THE API SHALL return a list of team members within the Visibility_Chain indicating whether each member submitted a Daily_Update on that date.
```

---

### C3 — Req 4: Add range heatmap AC (from S1)

**Location:** Requirement 4, after AC 8  
**Action:** Append new AC:

```
9. WHEN a Manager or Admin requests the Participation_Calendar for a date range not exceeding 90 days, THE API SHALL return a per-user, per-day submission matrix including weekend flags and the submission status for each day within the range.
```

---

### C4 — Req 4: Add points award AC (from S2)

**Location:** Requirement 4, after AC 9 (added above)  
**Action:** Append new AC:

```
10. WHEN an Employee successfully submits a Daily_Update, THE System SHALL award 5 points to the Employee's Points_Log with activity type `DailyUpdate`.
```

---

### C5 — Req 4: Add new DailyStatus values (from A1)

**Location:** Requirement 4, AC 2  
**Action:** Replace AC 2 with:

```
2. THE API SHALL accept the following Status values for a Daily_Update: `Open`, `InProgress`, `Completed`, `Blocked`, `FocusDay`, `Continuing`. The `FocusDay` status indicates intentional heads-down focused work with no new ticket activity. The `Continuing` status indicates ongoing work on a previously submitted ticket that spans multiple days.
```

---

### C6 — Req 4: Add FocusDay/Continuing behavior AC (from A1)

**Location:** Requirement 4, after AC 10 (added above)  
**Action:** Append new AC:

```
11. WHEN an Employee submits a Daily_Update with Status `FocusDay` or `Continuing`, THE API SHALL accept an empty TicketNumber and Description, and THE UI SHALL display these entries in a neutral color in the Participation_Calendar rather than treating them as missing submissions.
```

---

### C7 — Req 5 AC 5: Correct duplicate response behavior (from S3)

**Location:** Requirement 5, Acceptance Criteria 5  
**Action:** Replace AC 5 with:

```
5. IF an Employee submits a TaskResponse for a Task to which the Employee has already responded, THEN THE API SHALL update the existing response with the new Option and Remark values and return the updated record.
```

---

### C8 — Req 5: Add points award AC (from S4)

**Location:** Requirement 5, after AC 8  
**Action:** Append new AC:

```
9. WHEN an Employee submits a new TaskResponse (not an update to an existing response), THE System SHALL award 2 points to the Employee's Points_Log with activity type `TaskResponse`.
```

---

### C9 — Req 6: Add points award AC (from S5)

**Location:** Requirement 6, after AC 6  
**Action:** Append new AC:

```
7. WHEN an Employee successfully creates an Achievement, THE System SHALL award 10 points to the Employee's Points_Log with activity type `Achievement`.
```

---

### C10 — Req 6: Add file upload size limit AC (from S6)

**Location:** Requirement 6, after AC 7 (added above)  
**Action:** Append new AC:

```
8. WHEN an Employee uploads an achievement proof file larger than 10 MB, THE API SHALL return HTTP 413.
```

---

### C11 — Req 7 AC 1: Correct role restriction on Sales_Enquiry creation (from S7)

**Location:** Requirement 7, Acceptance Criteria 1  
**Action:** Replace AC 1 with:

```
1. WHEN an authenticated user creates a Sales_Enquiry with ClientName, Requirement, Technology, EnquiryDate, SalesCoordinator, and Status, THE API SHALL persist the record with ValidationStatus `Pending` and return the created entry.
```

---

### C12 — Req 8: Add enriched detail endpoint AC (from S8)

**Location:** Requirement 8, after AC 6  
**Action:** Append new AC:

```
7. WHEN a Manager or Admin requests the Validation_Queue with detail, THE API SHALL return each pending item enriched with the submitter's name, entity category, title, description, and proof URL.
```

---

### C13 — Req 9: Add AI narrative AC (from S9)

**Location:** Requirement 9, after AC 8  
**Action:** Append new AC:

```
9. WHEN a Manager, Admin, or HR user requests the narrative for a report, THE API SHALL return a human-readable summary derived from the report payload, including ticket count, participation rate, and notable achievements. THE API SHALL indicate in the response whether the narrative was AI-generated or derived from structured data.
```

---

### C14 — Req 12 AC 3: Correct duplicate MOM constraint (from S10)

**Location:** Requirement 12, Acceptance Criteria 3  
**Action:** Replace AC 3 with:

```
3. WHEN a Manager or Admin uploads a MOM for a Meeting, THE API SHALL persist the MOM_Entry linked to the Meeting, allowing multiple MOM entries per meeting to accumulate over time.
```

---

### C15 — Req 13: Replace Mon/Wed escalation with due-date-relative reminders (from A2)

**Location:** Requirement 13, Acceptance Criteria 6 and 7  
**Action:** Replace AC 6 and AC 7 with:

```
6. WHEN an Action_Item has Status `Open` or `InProgress` and the due date is 3 or fewer days away, THE System SHALL send a Zoho_Cliq DM reminder to the assignee if no reminder has been sent in the current reminder cycle.
7. WHEN an Action_Item has Status `Open` or `InProgress` and the due date is 1 day away, THE System SHALL send a second Zoho_Cliq DM reminder to the assignee.
8. WHEN an Action_Item's DueDate has passed and its Status is not `Completed` or `Cancelled`, THE System SHALL send a Zoho_Cliq DM escalation to the assignee's Manager indicating the item is overdue.
```

> **Note:** Renumber the existing AC 8 (Audit_Log) to AC 9 after inserting the new AC 8 above.

---

### C16 — Req 14: Add direct file upload ACs (from S11)

**Location:** Requirement 14, after AC 4  
**Action:** Append new ACs:

```
5. WHEN a Manager or Admin uploads a media file directly to an Event, THE API SHALL stream the file to Zoho_WorkDrive (maximum file size 20 MB), store the resulting URL as an EventMedia record, and return the created record.
6. IF the Zoho_WorkDrive upload fails or credentials are not configured, THE API SHALL return HTTP 502 and include a message suggesting the manual URL submission path as an alternative.
```

---

### C17 — Req 15 AC 3: Correct pagination claim (from S12)

**Location:** Requirement 15, Acceptance Criteria 3  
**Action:** Replace AC 3 with:

```
3. THE API SHALL support page-based pagination for the Activity_Feed, accepting `page` and `pageSize` query parameters with a maximum page size of 100 items per page.
```

---

### C18 — Req 16 AC 3: Specify badge criteria explicitly (from S13)

**Location:** Requirement 16, Acceptance Criteria 3  
**Action:** Replace AC 3 with:

```
3. THE System SHALL define three Badge types with the following monthly award criteria: `Consistent Contributor` is awarded when a user submits 20 or more Daily_Updates in a calendar month; `Team Player` is awarded when a user submits 15 or more TaskResponses in a calendar month; `Knowledge Sharer` is awarded when a user has 4 or more approved Achievements in a calendar month.
```

---

### C19 — Req 16: Add leaderboard visibility scoping (from A4)

**Location:** Requirement 16, after AC 5  
**Action:** Append new ACs:

```
6. WHEN a team member requests the monthly Leaderboard, THE API SHALL return the top 10 ranked users and the requesting user's own rank and points, without exposing the full ranked list of all team members.
7. WHEN a Manager or Admin requests the monthly Leaderboard, THE API SHALL return the full ranked list of all team members.
8. THE UI SHALL display each Employee's personal points trend for the past 3 calendar months on the Employee Dashboard, so that individual progress is visible independently of team ranking.
```

---

### C20 — Req 18 AC 2: Correct "past 4 weeks" to "past 7 days" (from S16)

**Location:** Requirement 18, Acceptance Criteria 2  
**Action:** Replace AC 2 with:

```
2. WHEN a Manager or Admin requests the dashboard, THE API SHALL return the daily Daily_Update submission trend for the past 7 days as a data series suitable for chart rendering.
```

---

### C21 — Req 19: Add notification preferences and quiet hours (from A6)

**Location:** Requirement 19, after AC 5  
**Action:** Append new ACs:

```
6. THE System SHALL respect shift quiet hours: no automated Zoho_Cliq DM or notification SHALL be sent before 14:00 IST or after 23:00 IST on any day.
7. THE System SHALL provide a daily digest option: when enabled for a user, all pending automated notifications for that day SHALL be consolidated into a single Zoho_Cliq DM sent at a configurable time rather than as separate messages.
```

---

### C22 — Req 20 AC 2: Change blocked nudge to go to employee first (from A3)

**Location:** Requirement 20, Acceptance Criteria 2  
**Action:** Replace AC 2 with:

```
2. EVERY day at 14:00 IST, THE System SHALL identify all Employees who have submitted a Daily_Update with Status `Blocked` for 3 or more consecutive days and send a Zoho_Cliq DM to the Employee asking if they need help or resources to become unblocked. IF the same Employee has been blocked for 5 or more consecutive days, THE System SHALL additionally send a Zoho_Cliq DM to the Employee's Manager.
```

---

### C23 — Req 20 AC 3: Reduce achievement pending nudge threshold (from A7)

**Location:** Requirement 20, Acceptance Criteria 3  
**Action:** Replace AC 3 with:

```
3. EVERY day at 14:00 IST, THE System SHALL identify all Achievements with ValidationStatus `Pending` that were created more than 3 days ago and send a Zoho_Cliq DM to the responsible Manager as a helpful reminder to review pending recognitions.
```

---

### C24 — Req 20 AC 4: Clarify nudge count as API-driven (from S17)

**Location:** Requirement 20, Acceptance Criteria 4  
**Action:** Replace AC 4 with:

```
4. THE API SHALL provide a lightweight endpoint returning the current count of stale enquiries, blocked ticket streaks, and pending achievements for the authenticated Manager's Visibility_Chain, so that the UI can render a navigation badge indicating the total number of active nudge conditions.
```

---

### C25 — Req 21: Add Inbox_Task category for FocusDay (from A1 + Req 21)

**Location:** Requirement 21, after AC 11  
**Action:** Append new AC:

```
12. WHEN an Inbox_Task is confirmed by a user, THE UI SHALL offer `FocusDay` and `Continuing` as selectable status options when the user subsequently submits a Daily_Update related to that task.
```

---

### C26 — Req 6: Add peer endorsement for achievements (from A7)

**Location:** Requirement 6, after AC 8 (added in C10)  
**Action:** Append new ACs:

```
9. WHEN an authenticated team member endorses a pending Achievement submitted by a colleague, THE System SHALL record the endorsement linked to that Achievement.
10. WHEN a pending Achievement has received 2 or more peer endorsements, THE System SHALL make the Achievement visible in the Activity_Feed with a `Peer Endorsed` indicator, without waiting for manager approval.
11. WHEN an Employee views the Activity_Feed, THE API SHALL include the Employee's own pending Achievements visible only to that Employee, so they can confirm their submission was received.
```

---

## Part 2 — New Requirement (from S14)

### C27 — Add Requirement 26: Master Data and Resource Allocation

**Location:** After Requirement 25 (Non-Functional Requirements)  
**Action:** Append the following new requirement section:

```markdown
### Requirement 26: Master Data and Resource Allocation

**User Story:** As a Manager or Admin, I want to manage project allocations and billing types for team members, so that resource utilization data in reports and dashboards is accurate.

#### Acceptance Criteria

1. WHEN an authenticated user requests the project list, THE API SHALL return all active projects ordered by project name.
2. WHEN an authenticated user requests the team list, THE API SHALL return all teams ordered by team name.
3. WHEN an authenticated user submits a resource allocation with UserId, ProjectId, BillingType, and StartDate, THE API SHALL create a new allocation record if none exists for that user-project combination, or update the existing active allocation if one exists.
4. THE API SHALL accept the following BillingType values for a resource allocation: `Billable`, `NonBillable`, `Shadow`, `Trainee`, `Overhead`.
5. THE API SHALL record an Audit_Log entry for every resource allocation creation or update.
```

---

## Part 3 — Non-Functional Addition (from S15)

### C28 — Req 25: Add write endpoint SLA

**Location:** Requirement 25, after AC 10  
**Action:** Append new AC:

```
11. THE API SHALL complete all write operations (create, update, bulk validation) within 2000 milliseconds under normal load (up to 50 concurrent users), excluding the duration of external Zoho API calls.
```

---

## Part 4 — Glossary Updates

### C29 — Update Glossary entries

**Location:** Glossary section  
**Action:** Apply the following glossary changes:

1. **Remove** the entry for `Compliance_Heatmap` (replaced by C1 above).

2. **Add** the following new entries after `Participation_Calendar`:

```
- **FocusDay**: A Daily_Update status indicating the team member spent the day in focused, uninterrupted work with no new ticket activity to report.
- **Continuing**: A Daily_Update status indicating the team member continued work on a previously submitted ticket with no new status change to report.
- **Peer_Endorsement**: A positive acknowledgement submitted by one team member for a colleague's Achievement, which can trigger early visibility in the Activity_Feed.
- **Streak**: A consecutive sequence of days on which a team member has submitted a Daily_Update, used as a personal progress indicator on the Employee Dashboard.
```

3. **Update** the `Smart_Nudge` entry to:

```
- **Smart_Nudge**: An automated Zoho_Cliq DM triggered by a stale or overdue condition, designed to prompt helpful action rather than assign blame.
```

---

## Summary of All Changes

| Change | Source | Requirement | Type |
|--------|--------|-------------|------|
| C1 | A5 | Glossary | Rename |
| C2 | A5 | Req 4 AC 8 | Rename + rewrite |
| C3 | S1 | Req 4 new AC 9 | Add |
| C4 | S2 | Req 4 new AC 10 | Add |
| C5 | A1 | Req 4 AC 2 | Replace |
| C6 | A1 | Req 4 new AC 11 | Add |
| C7 | S3 | Req 5 AC 5 | Replace |
| C8 | S4 | Req 5 new AC 9 | Add |
| C9 | S5 | Req 6 new AC 7 | Add |
| C10 | S6 | Req 6 new AC 8 | Add |
| C11 | S7 | Req 7 AC 1 | Replace |
| C12 | S8 | Req 8 new AC 7 | Add |
| C13 | S9 | Req 9 new AC 9 | Add |
| C14 | S10 | Req 12 AC 3 | Replace |
| C15 | A2 | Req 13 AC 6, 7 | Replace + add AC 8, renumber |
| C16 | S11 | Req 14 new AC 5, 6 | Add |
| C17 | S12 | Req 15 AC 3 | Replace |
| C18 | S13 | Req 16 AC 3 | Replace |
| C19 | A4 | Req 16 new AC 6, 7, 8 | Add |
| C20 | S16 | Req 18 AC 2 | Replace |
| C21 | A6 | Req 19 new AC 6, 7 | Add |
| C22 | A3 | Req 20 AC 2 | Replace |
| C23 | A7 | Req 20 AC 3 | Replace |
| C24 | S17 | Req 20 AC 4 | Replace |
| C25 | A1 | Req 21 new AC 12 | Add |
| C26 | A7 | Req 6 new AC 9, 10, 11 | Add |
| C27 | S14 | New Req 26 | Add |
| C28 | S15 | Req 25 new AC 11 | Add |
| C29 | A1/A3/A5 | Glossary | Add + update entries |

**Total changes: 29**  
**Requirements affected: 4, 5, 6, 7, 8, 9, 12, 13, 14, 15, 16, 18, 19, 20, 21, 25, Glossary**  
**New requirements added: 1 (Req 26)**
