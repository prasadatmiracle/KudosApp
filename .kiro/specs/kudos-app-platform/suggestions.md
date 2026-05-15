# Requirements Review — Suggestions

> Generated: 2026-05-15  
> Reviewed against: actual codebase (controllers, models, services)  
> Status: Pending incorporation into requirements.md

---

## Summary Table

| # | Requirement | Issue | Severity |
|---|-------------|-------|----------|
| 1 | Req 4 — Daily Updates | Compliance heatmap range endpoint missing | Medium |
| 2 | Req 4 — Daily Updates | Points award on submission not captured | Medium |
| 3 | Req 5 — Tasks | Duplicate response behavior contradicts code (should be update, not 409) | High |
| 4 | Req 5 — Tasks | Points award on new TaskResponse not captured | Medium |
| 5 | Req 6 — Achievements | Points award on creation not captured | Medium |
| 6 | Req 6 — Achievements | File upload size limit (10 MB) not specified | Medium |
| 7 | Req 7 — Sales | Sales enquiry creation role restriction is wrong (all roles can create) | Medium |
| 8 | Req 8 — Validation Queue | Enriched detail endpoint missing | Low |
| 9 | Req 9 — Weekly Reports | AI narrative endpoint missing | Low |
| 10 | Req 12 — Meetings | Duplicate MOM constraint contradicts code (multiple MOMs are allowed) | High |
| 11 | Req 14 — Events | Direct file upload endpoint and 20 MB size limit missing | Medium |
| 12 | Req 15 — Activity Feed | Total count / pagination envelope overstated (not returned by API) | Medium |
| 13 | Req 16 — Gamification | Badge award criteria not specified (thresholds are in code but not in doc) | Medium |
| 14 | Missing | Master Data and Resource Allocation has no requirement at all | High |
| 15 | Req 25 — Non-Functional | Write endpoint performance SLA missing | Low |
| 16 | Req 18 — Manager Dashboard | "past 4 weeks" should be "past 7 days" (code uses `today.AddDays(-6)`) | Low |
| 17 | Req 20 — Smart Nudges | AC 4 mixes UI concern into API requirement; should reference the counts endpoint | Low |

---

## Detailed Suggestions

---

### S1 — Req 4: Add compliance heatmap range endpoint

**Problem:** AC 8 only describes a single-day heatmap (`GET /api/daily-updates/compliance-heatmap`). The code also has a range endpoint (`GET /api/daily-updates/compliance-heatmap/range`) that returns a full per-user, per-day matrix with weekend flags and per-day status. This is the endpoint the calendar UI uses.

**Suggested addition to Req 4:**
> 9. WHEN a Manager or Admin requests the Compliance_Heatmap for a date range not exceeding 90 days, THE API SHALL return a per-user, per-day submission matrix including weekend flags and the submission status for each day within the range.

---

### S2 — Req 4: Add points award on Daily_Update submission

**Problem:** The code awards 5 points on every successful Daily_Update submission (`pointsService.AddPoints(userId, 5, "DailyUpdate", ...)`). Requirement 16 AC 1 references `DailyUpdate` as a point-earning activity but Requirement 4 never states this behavior.

**Suggested addition to Req 4:**
> 9 (or 10 if S1 is added). WHEN an Employee successfully submits a Daily_Update, THE System SHALL award 5 points to the Employee's Points_Log with activity type `DailyUpdate`.

---

### S3 — Req 5: Correct duplicate TaskResponse behavior

**Problem:** AC 5 states that a duplicate TaskResponse returns HTTP 409. The actual code **updates** the existing response (overwrites Option and Remark) rather than rejecting it. The requirement contradicts the implementation.

**Current AC 5:**
> IF an Employee attempts to submit more than one TaskResponse for the same Task, THEN THE API SHALL return HTTP 409 with a message indicating a duplicate response.

**Suggested replacement:**
> IF an Employee submits a TaskResponse for a Task to which the Employee has already responded, THEN THE API SHALL update the existing response with the new Option and Remark values and return the updated record.

---

### S4 — Req 5: Add points award on new TaskResponse

**Problem:** The code awards 2 points when a new TaskResponse is created (not on updates). This is not captured in Req 5.

**Suggested addition to Req 5:**
> 9. WHEN an Employee submits a new TaskResponse (not an update to an existing response), THE System SHALL award 2 points to the Employee's Points_Log with activity type `TaskResponse`.

---

### S5 — Req 6: Add points award on Achievement creation

**Problem:** The code awards 10 points when an Achievement is created. Not mentioned in Req 6.

**Suggested addition to Req 6:**
> 7. WHEN an Employee successfully creates an Achievement, THE System SHALL award 10 points to the Employee's Points_Log with activity type `Achievement`.

---

### S6 — Req 6: Add file upload size limit

**Problem:** The achievement proof upload endpoint has a 10 MB request size limit (`[RequestSizeLimit(10 * 1024 * 1024)]`). This is a testable constraint not captured in the requirements.

**Suggested addition to Req 6:**
> 8. WHEN an Employee uploads an achievement proof file larger than 10 MB, THE API SHALL return HTTP 413.

---

### S7 — Req 7: Correct Sales_Enquiry creation role restriction

**Problem:** AC 1 says "Manager or Admin creates a Sales_Enquiry" but `POST /api/sales/enquiries` has no `[Authorize(Roles = ...)]` attribute — any authenticated user (including Employees) can create one. Engagements and Sessions are Manager/Admin only, but enquiries are open to all roles.

**Current AC 1 (partial):**
> WHEN a Manager or Admin creates a Sales_Enquiry...

**Suggested correction:**
> WHEN an authenticated user creates a Sales_Enquiry with ClientName, Requirement, Technology, EnquiryDate, SalesCoordinator, and Status, THE API SHALL persist the record with ValidationStatus `Pending` and return the created entry.

---

### S8 — Req 8: Add enriched validation detail endpoint

**Problem:** The code has `GET /api/validations/pending-detail` which returns each pending item enriched with the submitter's name, category, title, description, and proof URL joined from the underlying entity. The basic `GET /api/validations/pending` only returns raw `ValidationRecord` objects. The UI uses the detail endpoint. This is not captured.

**Suggested addition to Req 8:**
> 7. WHEN a Manager or Admin requests the Validation_Queue with detail, THE API SHALL return each pending item enriched with the submitter's name, entity category, title, description, and proof URL.

---

### S9 — Req 9: Add AI narrative endpoint

**Problem:** The code has `GET /api/reports/{id}/narrative` which generates a human-readable summary from the report payload (stub today, Azure OpenAI later). This is a distinct feature not captured in any requirement.

**Suggested addition to Req 9:**
> 9. WHEN a Manager, Admin, or HR user requests the narrative for a report, THE API SHALL return a human-readable summary derived from the report payload, including ticket count, participation rate, and notable achievements. THE API SHALL indicate whether the narrative was AI-generated or derived from structured data.

---

### S10 — Req 12: Correct duplicate MOM constraint

**Problem:** AC 3 states that a second MOM upload returns HTTP 409. The actual code does **not** enforce this — there is no uniqueness check in `MeetingsController.UploadMom()`, so multiple MOM entries per meeting are allowed and accumulate.

**Current AC 3:**
> IF a MOM_Entry already exists for a Meeting, THEN THE API SHALL return HTTP 409 indicating a MOM has already been uploaded for that meeting.

**Suggested replacement:**
> WHEN a Manager or Admin uploads a MOM for a Meeting, THE API SHALL persist the MOM_Entry linked to the Meeting, allowing multiple MOM entries per meeting to accumulate over time.

---

### S11 — Req 14: Add direct file upload endpoint for events

**Problem:** The code has a second media endpoint `POST /api/events/{id}/media/upload` that accepts a multipart file upload (up to 20 MB), streams it to Zoho WorkDrive, and stores the returned URL. The current requirement only covers the URL-submission path (`POST /api/events/{id}/media`).

**Suggested additions to Req 14:**
> 5. WHEN a Manager or Admin uploads a media file directly to an Event, THE API SHALL stream the file to Zoho_WorkDrive (maximum file size 20 MB), store the resulting URL as an EventMedia record, and return the created record.
> 6. IF the Zoho_WorkDrive upload fails or credentials are not configured, THE API SHALL return HTTP 502 and include a message suggesting the manual URL submission path as an alternative.

---

### S12 — Req 15: Correct Activity Feed pagination claim

**Problem:** AC 3 states the response includes "total item count and current page metadata." The actual `FeedController` returns only the page items — no total count or pagination envelope is included in the response body.

**Current AC 3:**
> WHEN the Activity_Feed response is returned, THE API SHALL include the total item count and current page metadata to allow the UI to render pagination controls.

**Suggested replacement:**
> THE API SHALL support page-based pagination for the Activity_Feed, accepting `page` and `pageSize` query parameters with a maximum page size of 100 items per page.

---

### S13 — Req 16: Specify badge award criteria explicitly

**Problem:** AC 3 says the system "shall define three Badge types with documented award criteria" but never states what those criteria are. The code is explicit:
- `Consistent Contributor` = 20+ daily updates in the calendar month
- `Team Player` = 15+ task responses in the calendar month
- `Knowledge Sharer` = 4+ approved achievements in the calendar month

**Current AC 3:**
> THE System SHALL define three Badge types: `Consistent Contributor`, `Team Player`, and `Knowledge Sharer`, each with documented award criteria.

**Suggested replacement:**
> THE System SHALL define three Badge types with the following monthly award criteria: `Consistent Contributor` is awarded when a user submits 20 or more Daily_Updates in a calendar month; `Team Player` is awarded when a user submits 15 or more TaskResponses in a calendar month; `Knowledge Sharer` is awarded when a user has 4 or more approved Achievements in a calendar month.

---

### S14 — Missing Requirement: Master Data and Resource Allocation

**Problem:** The `MasterDataController` exposes three endpoints with no corresponding requirement:
- `GET /api/master-data/projects` — list all projects
- `GET /api/master-data/teams` — list all teams
- `POST /api/master-data/resource-allocation` — upsert a user's project allocation and billing type

Resource allocations feed directly into the monthly report's resource utilization section and the manager dashboard's billing type breakdown. This is a meaningful, testable feature area with no requirement.

**Suggested new Requirement 26: Master Data and Resource Allocation**

> **User Story:** As a Manager or Admin, I want to manage project allocations and billing types for team members, so that resource utilization data in reports and dashboards is accurate.
>
> **Acceptance Criteria:**
> 1. WHEN an authenticated user requests the project list, THE API SHALL return all active projects ordered by project name.
> 2. WHEN an authenticated user requests the team list, THE API SHALL return all teams ordered by team name.
> 3. WHEN an authenticated user submits a resource allocation with UserId, ProjectId, BillingType, and StartDate, THE API SHALL create a new allocation record if none exists for that user-project combination, or update the existing active allocation if one exists.
> 4. THE API SHALL accept the following BillingType values for a resource allocation: `Billable`, `NonBillable`, `Shadow`, `Trainee`, `Overhead`.
> 5. THE API SHALL record an Audit_Log entry for every resource allocation creation or update.

---

### S15 — Req 25: Add write endpoint performance SLA

**Problem:** AC 1 only covers read endpoints (500ms). Write operations such as report generation and bulk validation can be significantly heavier. The absence of any SLA for writes leaves a gap.

**Suggested addition to Req 25:**
> 11. THE API SHALL complete all write operations (create, update, bulk validation) within 2000 milliseconds under normal load (up to 50 concurrent users), excluding external Zoho API calls.

---

### S16 — Req 18: Correct "past 4 weeks" to "past 7 days"

**Problem:** AC 2 says the weekly trend covers "the past 4 weeks." The code uses `today.AddDays(-6)` which is a 7-day window (one week), not 4 weeks.

**Current AC 2 (partial):**
> ...THE API SHALL return the weekly Daily_Update submission trend for the past 4 weeks...

**Suggested correction:**
> ...THE API SHALL return the daily Daily_Update submission trend for the past 7 days as a data series suitable for chart rendering.

---

### S17 — Req 20: Clarify nudge count as API-driven, not internal state

**Problem:** AC 4 says "THE System SHALL increment the nudge count for the relevant navigation badge." This mixes a UI concern into an API requirement. The actual mechanism is that the UI calls `GET /api/nudges/counts` to get current nudge counts and renders the badge from that response — there is no server-side "increment" of a badge counter.

**Current AC 4:**
> WHEN a Smart_Nudge is sent, THE System SHALL increment the nudge count for the relevant navigation badge so the Manager sees a visual indicator of pending nudges in the UI.

**Suggested replacement:**
> THE API SHALL provide a lightweight endpoint returning the current count of stale enquiries, blocked ticket streaks, and pending achievements for the authenticated Manager's Visibility_Chain, so that the UI can render a navigation badge indicating the total number of active nudge conditions.

---

## How to Apply

All suggestions above can be incorporated into `requirements.md` by:
1. Updating the relevant acceptance criteria in-place for corrections (S3, S7, S10, S12, S13, S16, S17)
2. Appending new acceptance criteria to existing requirements (S1, S2, S4, S5, S6, S8, S9, S11, S15)
3. Adding a new Requirement 26 for master data (S14)
