# Requirements Document

## Introduction

KudosApp is a mobile-first team intelligence and engagement platform for a Microsoft Practice team of approximately 50 members (Developers and QA Engineers) operating on a 2 PM–11 PM IST shift. The platform integrates with the Zoho suite (Cliq, Mail, WorkDrive, Meetings) and is built on a .NET 9 Web API backend with a React/TypeScript frontend. It consolidates daily work tracking, achievement recognition, sales pipeline visibility, reporting, meeting management, action item tracking, and team culture activities into a single unified system.

---

## Glossary

- **System**: The KudosApp platform as a whole (backend API + frontend UI).
- **API**: The .NET 9 Web API backend.
- **UI**: The React + TypeScript frontend application.
- **Employee**: A team member with the `Employee` role — the base role for all team members.
- **Manager**: A team member with the `Manager` role who supervises one or more Employees.
- **Admin**: A team member with the `Admin` role who has full system access.
- **HR**: A team member with the `Hr` role who has read-only access to locked reports.
- **Visibility_Chain**: The hierarchical access path: Employee → Manager → Skip-Level Manager → Admin.
- **Daily_Update**: A work log entry submitted by an Employee for a specific project and date.
- **Task**: A structured item of type Vote, Action, or Info created by a Manager or Admin.
- **Achievement**: A self-reported accomplishment submitted by an Employee for manager validation.
- **Sales_Enquiry**: A potential client engagement record submitted for manager validation.
- **Engagement**: A confirmed client project record with position count.
- **Sales_Session**: A team training or knowledge-sharing session related to sales.
- **Validation_Queue**: The pending list of Achievements and Sales_Enquiries awaiting manager decision.
- **Validation_Record**: An immutable audit record of a validation decision.
- **Weekly_Report**: A report covering one calendar week of ticket activity and manager notes.
- **Monthly_Report**: A report covering one calendar month including resource utilization, achievements, sales, and events.
- **Quarterly_Report**: A report covering one calendar quarter with trend data.
- **Report_Lock**: The action of finalizing a report, making it read-only and triggering HR/Admin notification.
- **MOM**: Minutes of Meeting — a structured record of meeting summary and action items.
- **Action_Item**: A tracked task assigned to a team member with priority, due date, and escalation rules.
- **Activity_Feed**: A paginated, chronologically ordered stream of approved Achievements and Events.
- **Points_Log**: An immutable record of points awarded to a user for a specific activity.
- **Badge**: A recognition award granted to a user based on defined criteria.
- **Leaderboard**: A monthly ranking of users by accumulated points.
- **Participation_Calendar**: A calendar-style visualization of a team member's Daily_Update submission history, showing participation patterns over time.
- **Smart_Nudge**: An automated Zoho_Cliq DM triggered by a stale or overdue condition, designed to prompt helpful action rather than assign blame.
- **Inbox_Task**: A task automatically extracted from a Zoho Mail or Cliq message via AI.
- **Deduplication_Hash**: A SHA-256 hash of sender and keyword content used to prevent duplicate Inbox_Task creation within a 24-hour window.
- **Audit_Log**: An immutable, append-only record of all significant system actions.
- **Reminder_Cap**: The policy limiting automated reminders to a maximum of 2 per user per day.
- **Zoho_Cliq**: The team messaging platform used for channel notifications and bot DMs.
- **Zoho_Mail**: The email service used for report notifications and reminders.
- **Zoho_WorkDrive**: The cloud file storage used for achievement proofs and event media.
- **Zoho_Meetings**: The video conferencing service used for meeting creation and transcript ingestion.
- **PWA**: Progressive Web App — a web application with offline support and installability.
- **IST**: Indian Standard Time (UTC+5:30), the timezone for all scheduled operations.
- **FocusDay**: A Daily_Update status indicating the team member spent the day in focused, uninterrupted work with no new ticket activity to report.
- **Continuing**: A Daily_Update status indicating the team member continued work on a previously submitted ticket with no new status change to report.
- **Peer_Endorsement**: A positive acknowledgement submitted by one team member for a colleague's Achievement, which can trigger early visibility in the Activity_Feed.
- **Streak**: A consecutive sequence of days on which a team member has submitted a Daily_Update, used as a personal progress indicator on the Employee Dashboard.

---

## Requirements

### Requirement 1: Authentication and Session Management

**User Story:** As a team member, I want to log in using my Zoho account, so that I can access the platform securely without managing a separate password.

#### Acceptance Criteria

1. WHEN a user submits a valid Zoho OAuth access token and email address, THE API SHALL validate the token against Zoho's OAuth service and issue a signed JWT with a 24-hour expiry.
2. WHEN a user submits a Zoho access token for an email address not present in the system, THE API SHALL auto-provision a new user account with the `Employee` role assigned to the default team and manager, and include the new user's profile in the authentication response.
3. WHEN a request arrives at a protected endpoint without a valid JWT in the `Authorization: Bearer` header, THE API SHALL return HTTP 401.
4. WHEN a JWT has expired, THE API SHALL return HTTP 401 with a response body indicating token expiry.
5. IF the Zoho OAuth service is unreachable during login, THEN THE API SHALL return HTTP 503 with a descriptive error message.
6. THE API SHALL support a demo login mode where a token value of `"demo"` is accepted for development and testing purposes.
7. THE API SHALL record an Audit_Log entry for every successful login, including the authentication provider used.

---

### Requirement 2: User Management

**User Story:** As an Admin, I want to manage team member accounts, so that the platform accurately reflects the current team composition.

#### Acceptance Criteria

1. THE API SHALL enforce four roles: `Employee`, `Manager`, `Admin`, and `Hr`, with each user assigned exactly one role.
2. WHEN a Manager requests the user list, THE API SHALL return only users within the Manager's Visibility_Chain.
3. WHEN an Admin submits a CSV file for bulk user import, THE API SHALL parse each row and create or update user accounts, returning a count of imported rows.
4. IF a CSV import row contains an email address that already exists, THEN THE API SHALL update the existing user record rather than creating a duplicate.
5. IF a CSV import row is missing a required field (EmployeeId, Name, Email, Role, TeamId), THEN THE API SHALL skip that row.
6. WHEN an Admin deactivates a user, THE API SHALL set the user's `IsActive` flag to false and prevent that user from authenticating.
7. THE API SHALL record an Audit_Log entry for every user creation, update, and deactivation action.

---

### Requirement 3: Visibility Chain and Team Access

**User Story:** As a Manager, I want to view the work and status of my direct reports and their reports, so that I can monitor team health without seeing unrelated team data.

#### Acceptance Criteria

1. THE System SHALL implement a Visibility_Chain where an Employee can view only their own data, a Manager can view their direct reports' data and their reports' reports' data (skip-level), and an Admin can view all data.
2. WHEN a Manager requests team data, THE API SHALL return data for all users whose `ManagerId` resolves to the requesting Manager within two levels of the hierarchy.
3. WHEN an Admin requests team data, THE API SHALL return data for all active users regardless of team or manager assignment.
4. THE API SHALL enforce Visibility_Chain access on all endpoints that return user-specific data, returning HTTP 403 for out-of-chain requests.

---

### Requirement 4: Daily Updates

**User Story:** As an Employee, I want to submit my daily work log per project and ticket, so that my manager has visibility into my daily progress without feeling like I'm filling in a timesheet.

#### Acceptance Criteria

1. WHEN an Employee submits a Daily_Update with a valid ProjectId, WorkDate, TicketNumber, Description, and Status, THE API SHALL persist the record and return the created entry.
2. THE API SHALL accept the following Status values for a Daily_Update: `Open`, `InProgress`, `Completed`, `Blocked`, `FocusDay`, `Continuing`. The `FocusDay` status indicates intentional heads-down focused work with no new ticket activity. The `Continuing` status indicates ongoing work on a previously submitted ticket that spans multiple days. The legacy `NoTask` value SHALL remain accepted for backward compatibility.
3. IF an Employee submits a Daily_Update with a TicketNumber and WorkDate combination that already exists for that user, THEN THE API SHALL return HTTP 409 with a message indicating a duplicate entry.
4. WHEN an Employee submits a Daily_Update with Status `FocusDay`, `Continuing`, or `NoTask`, THE API SHALL accept an empty TicketNumber and Description and SHALL generate a synthetic ticket identifier internally.
5. WHEN a Manager or Admin requests daily updates for their Visibility_Chain, THE API SHALL return all updates filtered by the requested date range.
6. THE UI SHALL persist an in-progress Daily_Update form to browser localStorage as an offline draft, restoring it automatically on next page load.
7. WHEN the UI detects network connectivity after an offline period, THE UI SHALL prompt the user to submit any pending offline drafts.
8. WHEN a Manager or Admin requests the Participation_Calendar for a single date, THE API SHALL return a list of team members within the Visibility_Chain indicating whether each member submitted a Daily_Update on that date.
9. WHEN a Manager or Admin requests the Participation_Calendar for a date range not exceeding 90 days, THE API SHALL return a per-user, per-day submission matrix including weekend flags and the submission status for each day within the range.
10. WHEN an Employee successfully submits a Daily_Update, THE System SHALL award 5 points to the Employee's Points_Log with activity type `DailyUpdate`.
11. WHEN an Employee submits a Daily_Update with Status `FocusDay` or `Continuing`, THE UI SHALL display these entries in a neutral or positive color in the Participation_Calendar rather than treating them as missing submissions.
12. THE UI SHALL display a Streak indicator on the Employee Dashboard showing the number of consecutive days the Employee has submitted a Daily_Update.

---

### Requirement 5: Tasks and Polls

**User Story:** As a Manager, I want to create tasks and polls for my team, so that I can gather structured responses and assign actions.

#### Acceptance Criteria

1. WHEN a Manager or Admin creates a Task with a Title, Description, TaskType, and DueAtUtc, THE API SHALL persist the Task with State `Active` and return the created record.
2. THE API SHALL accept the following TaskType values: `Vote`, `Action`, `Info`.
3. WHEN an Employee submits a TaskResponse with a selected Option and optional Remark for an Active Task, THE API SHALL persist the response and return the created record.
4. IF an Employee attempts to submit a TaskResponse for a Task in State `Closed`, THEN THE API SHALL return HTTP 409 with a message indicating the task is closed.
5. IF an Employee submits a TaskResponse for a Task to which the Employee has already responded, THEN THE API SHALL update the existing response with the new Option and Remark values and return the updated record.
6. WHEN a Manager or Admin closes a Task, THE API SHALL update the Task State to `Closed`.
7. WHEN a Manager or Admin requests the voting report for a Task, THE API SHALL return all TaskResponses with respondent names, selected options, remarks, and submission timestamps.
8. WHEN a Manager or Admin exports the voting report, THE API SHALL return the report as a downloadable artifact.
9. WHEN an Employee submits a new TaskResponse (not an update to an existing response), THE System SHALL award 2 points to the Employee's Points_Log with activity type `TaskResponse`.

---

### Requirement 6: Achievements and Knowledge Sharing

**User Story:** As an Employee, I want to submit my achievements for recognition, so that my contributions are acknowledged promptly and visible to the team.

#### Acceptance Criteria

1. WHEN an Employee submits an Achievement with a Category, Title, Description, and optional ProofWorkDriveUrl, THE API SHALL persist the record with ValidationStatus `Pending` and return the created entry.
2. WHEN a Manager or Admin approves an Achievement, THE API SHALL update the ValidationStatus to `Approved`, record the ValidatedByUserId and ValidatedAtUtc, and create a Validation_Record.
3. WHEN a Manager or Admin rejects an Achievement, THE API SHALL update the ValidationStatus to `Rejected`, record the ValidatedByUserId, ValidatedAtUtc, and rejection remarks, and create a Validation_Record.
4. WHEN an Employee uploads an achievement proof file, THE API SHALL upload the file to Zoho_WorkDrive and store the resulting file URL in the Achievement record.
5. WHEN the Activity_Feed is requested, THE API SHALL include Achievements with ValidationStatus `Approved` and Achievements with 2 or more Peer_Endorsements (displayed with a `Peer Endorsed` indicator).
6. THE API SHALL record an Audit_Log entry for every Achievement approval and rejection action.
7. WHEN an Employee successfully creates an Achievement, THE System SHALL award 10 points to the Employee's Points_Log with activity type `Achievement`.
8. WHEN an Employee uploads an achievement proof file larger than 10 MB, THE API SHALL return HTTP 413.
9. WHEN an authenticated team member endorses a pending Achievement submitted by a colleague, THE System SHALL record the Peer_Endorsement linked to that Achievement.
10. WHEN a pending Achievement has received 2 or more Peer_Endorsements, THE System SHALL make the Achievement visible in the Activity_Feed with a `Peer Endorsed` indicator, without waiting for manager approval.
11. WHEN an Employee views the Activity_Feed, THE API SHALL include the Employee's own pending Achievements visible only to that Employee, so they can confirm their submission was received.

---

### Requirement 7: Sales Module

**User Story:** As a team member, I want to track sales enquiries and client engagements, so that the team's business development activity is visible in reports.

#### Acceptance Criteria

1. WHEN an authenticated user creates a Sales_Enquiry with ClientName, Requirement, Technology, EnquiryDate, SalesCoordinator, and Status, THE API SHALL persist the record with ValidationStatus `Pending` and return the created entry.
2. WHEN a Manager or Admin approves a Sales_Enquiry, THE API SHALL update the ValidationStatus to `Approved` and create a Validation_Record.
3. WHEN a Manager or Admin rejects a Sales_Enquiry, THE API SHALL update the ValidationStatus to `Rejected` with remarks and create a Validation_Record.
4. WHEN a Manager or Admin creates an Engagement with ClientName, ProjectName, NumberOfPositions, and Details, THE API SHALL persist the record and return the created entry.
5. WHEN a Manager or Admin creates a Sales_Session with Title, SessionDate, TeamId, and Description, THE API SHALL persist the record and return the created entry.
6. THE API SHALL include approved Sales_Enquiries, Engagements, and Sales_Sessions in Monthly_Report generation.

---

### Requirement 8: Validation Queue

**User Story:** As a Manager, I want a single queue of pending items requiring my decision, so that I can efficiently approve or reject achievements and sales enquiries.

#### Acceptance Criteria

1. WHEN a Manager or Admin requests the Validation_Queue, THE API SHALL return all Achievements and Sales_Enquiries with ValidationStatus `Pending` that are within the requester's Visibility_Chain.
2. WHEN a Manager or Admin submits a single validation decision with a Status and optional Remarks, THE API SHALL update the target entity's ValidationStatus and create an immutable Validation_Record.
3. WHEN a Manager or Admin submits a bulk validation decision with a list of ValidationRecordIds, a Status, and optional Remarks, THE API SHALL apply the decision to all listed records in a single transaction.
4. IF a bulk validation decision references a ValidationRecordId that does not exist or is outside the requester's Visibility_Chain, THEN THE API SHALL skip that record and include it in the response's failure list.
5. THE API SHALL record an Audit_Log entry for every single and bulk validation decision.
6. THE System SHALL prevent modification of a Validation_Record after it has been created, preserving an immutable audit trail.
7. WHEN a Manager or Admin requests the Validation_Queue with detail, THE API SHALL return each pending item enriched with the submitter's name, entity category, title, description, proof URL, and Peer_Endorsement count.

---

### Requirement 9: Reporting — Weekly

**User Story:** As a Manager, I want to generate and edit weekly reports, so that I can submit a finalized record of the team's weekly activity to stakeholders.

#### Acceptance Criteria

1. WHEN a Manager or Admin requests weekly report generation for a date range, THE API SHALL aggregate all Daily_Updates within that range for the requester's Visibility_Chain, deduplicate by project and ticket number, and return a WeeklyReportPayload.
2. WHILE a Weekly_Report has Status `Draft`, THE API SHALL allow the generating Manager or Admin to edit the ManagerNotes field.
3. WHEN a Manager or Admin locks a Weekly_Report, THE API SHALL update the Status to `Locked`, prevent further edits, and send an email notification via Zoho_Mail to all users with the `Hr` and `Admin` roles.
4. WHEN an Admin reopens a locked Weekly_Report, THE API SHALL update the Status back to `Draft`, allowing edits to resume.
5. WHEN an HR user requests a Weekly_Report, THE API SHALL return the report only if its Status is `Locked`.
6. WHEN a Manager or Admin exports a Weekly_Report as Excel, THE API SHALL return the report as a downloadable XLSX artifact.
7. WHEN a Manager or Admin exports a Weekly_Report as text, THE API SHALL return the report as a downloadable TXT artifact.
8. EVERY Friday at 18:00 IST, THE System SHALL automatically generate a Draft Weekly_Report for the current week for each active Manager.
9. WHEN a Manager, Admin, or HR user requests the narrative for a report, THE API SHALL return a human-readable summary derived from the report payload, including ticket count, participation rate, and notable achievements. THE API SHALL indicate in the response whether the narrative was AI-generated or derived from structured data.

---

### Requirement 10: Reporting — Monthly

**User Story:** As a Manager, I want to generate monthly reports that consolidate all team activity, so that leadership has a comprehensive view of the month's output.

#### Acceptance Criteria

1. WHEN a Manager or Admin requests monthly report generation for a given month, THE API SHALL aggregate resource utilization, approved Achievements, approved Sales_Enquiries, Engagements, Sales_Sessions, and Events for that month and return a MonthlyReportSection.
2. WHEN a Manager or Admin locks a Monthly_Report, THE API SHALL update the Status to `Locked` and send an email notification via Zoho_Mail to all users with the `Hr` and `Admin` roles.
3. WHEN a Manager or Admin exports a Monthly_Report as Excel, THE API SHALL return the report as a downloadable XLSX artifact.
4. WHEN a Manager or Admin exports a Monthly_Report as a presentation, THE API SHALL return the report as a downloadable PPTX artifact with sections for resource utilization, achievements, sales pipeline, events, meetings, KPIs, team performance, action items, and next-month goals.
5. ON the last calendar day of each month at 18:00 IST, THE System SHALL automatically assemble a Draft Monthly_Report for the completed month for each active Manager and send an email notification to all HR and Admin users.

---

### Requirement 11: Reporting — Quarterly

**User Story:** As a Manager, I want quarterly trend reports, so that I can identify patterns in team performance over time.

#### Acceptance Criteria

1. WHEN a Manager or Admin requests quarterly report generation for a given year and quarter, THE API SHALL aggregate enquiry counts, achievement counts, and participation rates by month and return a QuarterlyReportSection.
2. WHEN a Manager or Admin exports a Quarterly_Report as Excel, THE API SHALL return the report as a downloadable XLSX artifact.
3. WHEN a Manager or Admin exports a Quarterly_Report as text, THE API SHALL return the report as a downloadable TXT artifact.

---

### Requirement 12: Meetings and Minutes of Meeting

**User Story:** As a Manager, I want to create meeting records and upload minutes, so that meeting outcomes and action items are tracked in the system.

#### Acceptance Criteria

1. WHEN an authenticated user creates a Meeting with a Title, MeetingAtUtc, and ZohoMeetingUrl, THE API SHALL persist the record and return the created entry.
2. WHEN an authenticated user uploads a MOM for a Meeting with a Summary and ActionItems text, THE API SHALL persist the MOM_Entry linked to the Meeting, allowing multiple MOM entries per meeting to accumulate over time.
3. WHEN a transcript is submitted to the transcript ingestion endpoint for a Meeting, THE API SHALL pass the transcript text to the AI extraction service, extract a Summary and structured ActionItems, and persist the result as a MOM_Entry.
4. WHEN the AI extraction service is unavailable during transcript ingestion, THE API SHALL return HTTP 503 and not persist a partial MOM_Entry.

---

### Requirement 13: Action Items

**User Story:** As a Manager, I want to create and assign action items to team members with fair escalation rules, so that follow-up tasks are tracked without creating a micromanagement dynamic.

#### Acceptance Criteria

1. WHEN a Manager or Admin creates an Action_Item with Title, Description, AssignedToUserId, DueDate, and Priority, THE API SHALL persist the record with Status `Open` and return the created entry.
2. THE API SHALL accept the following Priority values for an Action_Item: `Low`, `Medium`, `High`, `Critical`.
3. THE API SHALL accept the following Status values for an Action_Item: `Open`, `InProgress`, `Completed`, `Cancelled`.
4. WHEN an assignee or Manager updates an Action_Item's Status to `Completed`, THE API SHALL record the CompletedAtUtc timestamp.
5. WHEN a Manager or Admin requests the Action_Item list, THE API SHALL return all Action_Items within the requester's Visibility_Chain, including an `IsOverdue` flag set to true for items where DueDate is before the current date and Status is not `Completed` or `Cancelled`.
6. WHEN an Action_Item has Status `Open` or `InProgress` and the due date is 3 or fewer days away, THE System SHALL send a Zoho_Cliq DM reminder to the assignee if no reminder has been sent in the current reminder cycle.
7. WHEN an Action_Item has Status `Open` or `InProgress` and the due date is 1 day away, THE System SHALL send a second Zoho_Cliq DM reminder to the assignee.
8. WHEN an Action_Item's DueDate has passed and its Status is not `Completed` or `Cancelled`, THE System SHALL send a Zoho_Cliq DM escalation to the assignee's Manager indicating the item is overdue.
9. THE API SHALL record an Audit_Log entry for every Action_Item creation and status change.
10. THE API SHALL support a SourceType field on Action_Items indicating origin: `Manual`, `MOM`, or `Meeting`, with an optional SourceId linking to the originating record.

---

### Requirement 14: Events and Culture

**User Story:** As a Manager or Admin, I want to create team events and attach media, so that cultural activities are documented and visible in the activity feed.

#### Acceptance Criteria

1. WHEN an authenticated user creates an Event with Title, Description, EventDate, and Location, THE API SHALL persist the record and return the created entry.
2. WHEN an authenticated user attaches a WorkDrive media URL to an Event, THE API SHALL store the URL in an EventMedia record linked to the Event and return the created record.
3. WHEN an authenticated user uploads a media file directly to an Event, THE API SHALL stream the file to Zoho_WorkDrive (maximum file size 20 MB), store the resulting URL as an EventMedia record, and return the created record.
4. IF an authenticated user attempts to attach more than 10 media links to a single Event, THEN THE API SHALL return HTTP 409 with a message indicating the maximum of 10 media links has been reached.
5. IF the Zoho_WorkDrive upload fails or credentials are not configured, THE API SHALL return HTTP 502 and include a message suggesting the manual URL submission path as an alternative.
6. WHEN the Activity_Feed is requested, THE API SHALL include all Events ordered by EventDate descending.

---

### Requirement 15: Activity Feed

**User Story:** As a team member, I want to see a unified feed of achievements and events, so that I can stay informed about team accomplishments and activities.

#### Acceptance Criteria

1. WHEN a user requests the Activity_Feed, THE API SHALL return a paginated list combining approved Achievements, Peer_Endorsed Achievements, and Events, ordered by creation date descending.
2. THE API SHALL support page-based pagination for the Activity_Feed, accepting `page` and `pageSize` query parameters with a maximum page size of 100 items per page.

---

### Requirement 16: Performance Tracking and Gamification

**User Story:** As a team member, I want to earn points and badges for my contributions, so that I am motivated to participate consistently without feeling ranked against colleagues.

#### Acceptance Criteria

1. WHEN a user completes a point-earning activity, THE API SHALL create a Points_Log entry recording the user, points awarded, activity type, and reference ID. Point values: Daily_Update submission = 5 pts, new TaskResponse = 2 pts, Achievement creation = 10 pts.
2. WHEN a Manager or Admin requests the monthly Leaderboard, THE API SHALL return the full ranked list of all team members by total points for the requested month.
3. WHEN a team member requests the monthly Leaderboard, THE API SHALL return the top 10 ranked users and the requesting user's own rank and points, without exposing the full ranked list of all team members.
4. THE System SHALL define three Badge types with the following monthly award criteria: `Consistent Contributor` is awarded when a user submits 20 or more Daily_Updates in a calendar month; `Team Player` is awarded when a user submits 15 or more TaskResponses in a calendar month; `Knowledge Sharer` is awarded when a user has 4 or more approved Achievements in a calendar month.
5. WHEN a Manager or Admin triggers a badge refresh, THE API SHALL evaluate all active users' activity against each Badge's criteria and award badges to eligible users who have not yet received them.
6. THE API SHALL record an Audit_Log entry for every badge award.
7. THE UI SHALL display each Employee's personal points trend for the past 3 calendar months on the Employee Dashboard, so that individual progress is visible independently of team ranking.
8. THE UI SHALL display a Streak indicator showing the number of consecutive days the Employee has submitted a Daily_Update.

---

### Requirement 17: Employee Dashboard

**User Story:** As an Employee, I want a personal dashboard showing my key metrics, so that I can quickly assess my standing and pending tasks.

#### Acceptance Criteria

1. WHEN an Employee requests the dashboard, THE API SHALL return: the count of Active Tasks the Employee has not yet responded to, the submission status of today's Daily_Update, the Employee's total points for the current month, and the Employee's current Leaderboard rank.
2. WHEN an Employee requests the dashboard and has not submitted a Daily_Update for the current date, THE API SHALL include a flag indicating the update is missing.
3. THE UI SHALL display a quick-submit button for the Daily_Update directly on the Employee Dashboard to reduce navigation friction.

---

### Requirement 18: Manager and Admin Dashboard

**User Story:** As a Manager or Admin, I want a team health dashboard, so that I can identify risks and take action before they escalate.

#### Acceptance Criteria

1. WHEN a Manager or Admin requests the team health dashboard, THE API SHALL return: team participation percentage for today, the list of team members who have not submitted a Daily_Update today, the count of team members with a `Blocked` status ticket today, the count of pending Validation_Queue items, the count of open and overdue Action_Items, and the billing type breakdown for the team.
2. WHEN a Manager or Admin requests the team health dashboard, THE API SHALL return the daily Daily_Update submission trend for the past 7 days as a data series suitable for chart rendering.
3. WHEN a Manager or Admin requests the team health dashboard, THE API SHALL return a team engagement score calculated as: participation rate × 40% + task response rate × 30% + validation throughput × 30%.

---

### Requirement 19: Notifications and Scheduled Reminders

**User Story:** As a Manager, I want automated reminders sent to my team within shift hours, so that compliance and deadlines are maintained without overwhelming people with messages.

#### Acceptance Criteria

1. EVERY weekday at 17:00 IST, THE System SHALL identify all active Employees who have not submitted a Daily_Update for the current date and send each a Zoho_Cliq DM reminder.
2. THE System SHALL enforce the Reminder_Cap policy: no more than 2 automated reminder messages SHALL be sent to any single user on any single calendar day across all reminder types.
3. EVERY weekday at 22:00 IST, THE System SHALL send a Zoho_Cliq DM compliance digest to each active Manager listing the names of their direct reports who have not submitted a Daily_Update for the current date.
4. WHEN a report is locked, THE System SHALL send a Zoho_Mail email notification to all users with the `Hr` and `Admin` roles within 60 seconds of the lock action.
5. THE System SHALL send Zoho_Cliq channel notifications for significant team events including Achievement approvals and new Task creation.
6. THE System SHALL respect shift quiet hours: no automated Zoho_Cliq DM or notification SHALL be sent before 14:00 IST or after 23:00 IST on any day.
7. THE System SHALL provide a daily digest option: when enabled for a user, all pending automated notifications for that day SHALL be consolidated into a single Zoho_Cliq DM sent at a configurable time rather than as separate messages.

---

### Requirement 20: Smart Nudges

**User Story:** As a Manager, I want the system to proactively surface stale or problematic situations, so that I can offer help before issues compound.

#### Acceptance Criteria

1. EVERY weekday at 15:30 IST, THE System SHALL identify all Sales_Enquiries with ValidationStatus `Pending` that have not been updated in more than 7 days and send a Zoho_Cliq DM to the Sales_Enquiry owner as a helpful prompt.
2. EVERY weekday at 15:30 IST, THE System SHALL identify all Employees who have submitted a Daily_Update with Status `Blocked` for 3 or more consecutive days and send a Zoho_Cliq DM to the Employee asking if they need help or resources. IF the same Employee has been blocked for 5 or more consecutive days, THE System SHALL additionally send a Zoho_Cliq DM to the Employee's Manager.
3. EVERY weekday at 15:30 IST, THE System SHALL identify all Achievements with ValidationStatus `Pending` that were created more than 3 days ago and send a Zoho_Cliq DM to the responsible Manager as a helpful reminder to review pending recognitions.
4. THE API SHALL provide a lightweight endpoint returning the current count of stale enquiries, blocked ticket streaks, and pending achievements for the authenticated Manager's Visibility_Chain, so that the UI can render a navigation badge indicating the total number of active nudge conditions.
5. THE System SHALL enforce the Reminder_Cap policy for Smart_Nudges, counting them against the 2-per-user-per-day limit.

---

### Requirement 21: Smart Inbox Task Capture

**User Story:** As an Employee, I want action items from my Zoho Mail and Cliq messages to be automatically captured for my review, so that I do not miss follow-up tasks buried in communications.

#### Acceptance Criteria

1. WHEN a Zoho_Mail or Zoho_Cliq webhook delivers a message payload to the ingestion endpoint, THE System SHALL pass the message content to the AI extraction service and extract candidate action items.
2. WHEN the AI extraction service identifies an action item in a message, THE System SHALL compute a Deduplication_Hash from the sender and extracted keyword content and create an Inbox_Task with State `PendingConfirmation` only if no existing Inbox_Task has the same hash within the past 24 hours.
3. WHEN a user confirms an Inbox_Task, THE System SHALL transition the Inbox_Task State from `PendingConfirmation` to `Active`.
4. WHEN a user dismisses an Inbox_Task, THE System SHALL transition the Inbox_Task State to `Dismissed`.
5. WHEN a user marks an Inbox_Task as in progress, THE System SHALL transition the State from `Active` to `InProgress`.
6. WHEN a user marks an Inbox_Task as complete, THE System SHALL transition the State to `Completed` and record the CompletedAtUtc timestamp.
7. THE System SHALL create Inbox_Tasks as private by default, visible only to the owning user.
8. WHEN a user makes an Inbox_Task public and adds dependent users, THE System SHALL notify each dependent user via Zoho_Cliq DM when the Inbox_Task is marked complete.
9. WHEN a user opts to include an Inbox_Task in the weekly report, THE System SHALL include the task text and category in the next Weekly_Report generation for that user.
10. WHEN a user schedules a reminder for an Inbox_Task, THE System SHALL send the reminder via the selected channel (`InApp`, `Cliq`, or `Both`) at the specified time.
11. IF the AI extraction service is unavailable during webhook ingestion, THEN THE System SHALL queue the message for retry and return HTTP 202 to the webhook caller.
12. WHEN a Manager or Admin requests the team Inbox_Task view, THE API SHALL return all non-private Inbox_Tasks for users within the Visibility_Chain, filterable by state.

---

### Requirement 22: Governance and Audit Log

**User Story:** As an Admin, I want a complete, immutable audit trail of all significant actions, so that I can investigate incidents and demonstrate compliance.

#### Acceptance Criteria

1. THE System SHALL record an Audit_Log entry for every significant action including: user login, user creation and deactivation, validation decisions, report lock and reopen, badge awards, Action_Item creation and status changes, resource allocation changes, and bulk operations.
2. WHEN an Admin requests the Audit_Log, THE API SHALL return a paginated list of Audit_Log entries ordered by creation time descending, including the actor's user ID, action type, entity type, entity ID, metadata, and timestamp.
3. THE System SHALL prevent modification or deletion of any Audit_Log entry after it has been created.
4. WHEN a non-Admin user attempts to access the Audit_Log endpoint, THE API SHALL return HTTP 403.

---

### Requirement 23: Zoho Integrations

**User Story:** As a system operator, I want all Zoho integrations to degrade gracefully when credentials are missing, so that the application remains functional during initial setup.

#### Acceptance Criteria

1. WHEN Zoho_Cliq channel webhook credentials are configured, THE System SHALL deliver channel notifications by posting to the configured webhook URL.
2. WHEN Zoho_Cliq bot credentials are configured, THE System SHALL deliver direct messages by calling the Zoho Cliq bot DM API.
3. WHEN Zoho_Mail credentials are configured, THE System SHALL send emails by calling the Zoho Mail API using the configured OAuth refresh token.
4. WHEN Zoho_WorkDrive credentials are configured, THE System SHALL upload files by calling the Zoho WorkDrive API and return the resulting file URL.
5. IF any Zoho integration credential is missing or empty at the time of a notification or upload attempt, THEN THE System SHALL log a warning and skip the integration call without returning an error to the caller.
6. WHEN a Zoho OAuth access token expires during an API call, THE System SHALL use the configured refresh token to obtain a new access token and retry the call once before returning an error.

---

### Requirement 24: PWA and Offline Support

**User Story:** As a mobile user, I want to use the app offline and install it on my home screen, so that I can submit updates even when connectivity is intermittent.

#### Acceptance Criteria

1. THE UI SHALL include a service worker that caches the application shell (HTML, CSS, JavaScript) for offline access.
2. THE UI SHALL include a web app manifest with app name, icons, and display mode set to `standalone`, enabling "Add to Home Screen" on mobile browsers.
3. WHILE the device is offline, THE UI SHALL allow users to compose and save a Daily_Update draft to browser localStorage.
4. WHEN the device regains network connectivity, THE UI SHALL detect the reconnection and prompt the user to submit any pending offline drafts.
5. THE UI SHALL display a visual indicator when the application is operating in offline mode.

---

### Requirement 25: Non-Functional Requirements

**User Story:** As a system operator, I want the platform to meet baseline performance, security, and reliability standards, so that it is suitable for production use by a 50-member team.

#### Acceptance Criteria

1. THE API SHALL respond to all read endpoints within 500 milliseconds under normal load (up to 50 concurrent users).
2. THE API SHALL complete all write operations (create, update, bulk validation) within 2000 milliseconds under normal load (up to 50 concurrent users), excluding the duration of external Zoho API calls.
3. THE API SHALL use parameterized queries for all database interactions to prevent SQL injection.
4. THE API SHALL validate all input payloads and return HTTP 400 with field-level error details for invalid requests.
5. THE System SHALL store all JWT signing keys and Zoho OAuth credentials in environment-specific configuration, not in source-controlled files.
6. THE API SHALL enforce role-based access control on every endpoint, returning HTTP 403 when the authenticated user's role does not permit the requested operation.
7. THE System SHALL use HTTPS for all client-server and server-to-Zoho communications.
8. THE API SHALL support SQL Server as the production database with EF Core migrations managing schema changes.
9. THE UI SHALL be responsive and usable on mobile devices with screen widths from 375px upward.
10. THE System SHALL log all unhandled exceptions with sufficient context (endpoint, user ID, timestamp) to support incident investigation.
11. THE API SHALL support graceful shutdown, completing in-flight requests before terminating.
12. THE System SHALL skip weekend days (Saturday and Sunday) for all scheduled reminder and digest jobs.

---

### Requirement 26: Master Data and Resource Allocation

**User Story:** As a Manager or Admin, I want to manage project allocations and billing types for team members, so that resource utilization data in reports and dashboards is accurate.

#### Acceptance Criteria

1. WHEN an authenticated user requests the project list, THE API SHALL return all active projects ordered by project name.
2. WHEN an authenticated user requests the team list, THE API SHALL return all teams ordered by team name.
3. WHEN an authenticated user submits a resource allocation with UserId, ProjectId, BillingType, and StartDate, THE API SHALL create a new allocation record if none exists for that user-project combination, or update the existing active allocation if one exists.
4. THE API SHALL accept the following BillingType values for a resource allocation: `Billable`, `NonBillable`, `Shadow`, `Trainee`, `Overhead`.
5. THE API SHALL record an Audit_Log entry for every resource allocation creation or update.
