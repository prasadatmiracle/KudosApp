# Kudos App Starter Implementation

This repository now includes a working starter implementation for the planned **mobile-first, Zoho-integrated team engagement and reporting platform**.

## What Is Implemented

### Backend (`.NET 9 Web API`)
- JWT auth with Zoho SSO starter endpoint (`/api/auth/zoho-sso`).
- Role model and visibility chain support (employee -> manager -> skip-level -> admin).
- Operations APIs:
  - Daily updates (with `NoTask` quick mode and duplicate ticket/day guard).
  - Tasks/polls with vote + remark response and voting report.
- Validation APIs:
  - Pending queue.
  - Single and bulk manager decisions.
- Reporting APIs:
  - Weekly/monthly/quarterly generation.
  - Weekly edit and lock-on-submit.
  - Admin reopen.
  - Export artifact (Excel-style CSV or text payload).
- Meetings APIs:
  - Meeting creation.
  - MOM manual upload.
  - Transcript ingest (stubbed extraction hook).
- Knowledge/Culture & Business APIs:
  - Achievements feed and sales modules.
  - Events with WorkDrive link attachments (max 10 media links/event).
- Governance:
  - Immutable audit log endpoint.
  - Reminder cap policy (max 2 reminders/user/day).
- Performance:
  - Points tracking and monthly leaderboard.
  - Badge refresh endpoint.

### Frontend (No npm, static `wwwroot`)
- Mobile-first UI served directly by ASP.NET Core static files.
- Implemented screens:
  - Login (Zoho SSO starter mode)
  - Dashboard
  - Tasks/Voting
  - Daily Update (with offline draft in localStorage)
  - Feed (paginated)
  - Leaderboard
  - Validation Queue
  - Reports
  - Profile
- Uses plain HTML/CSS/JavaScript and calls `/api/*` directly.

### Database
- `src/backend/schema.sql` includes SQL Server schema for production migration.
- Current running backend persists in memory for quick pilot bootstrapping.

## Run Backend

From repository root:

```powershell
$env:APPDATA='C:\Users\pnagabathula\Prasad\AI\KudosApp\.nuget\appdata'
$env:USERPROFILE='C:\Users\pnagabathula\Prasad\AI\KudosApp\.nuget\user'
$env:DOTNET_CLI_HOME='C:\Users\pnagabathula\Prasad\AI\KudosApp\.dotnet'
$env:NUGET_PACKAGES='C:\Users\pnagabathula\Prasad\AI\KudosApp\.nuget\packages'
dotnet run --project src\backend\KudosApp.Api\KudosApp.Api.csproj
```

Seed users for quick login:
- `admin@kudos.local`
- `manager@kudos.local`
- `employee@kudos.local`
- `hr@kudos.local`

Use any non-empty token in login request for starter mode.

## Run UI (No npm required)

The UI is hosted by the backend. Just run:

```powershell
dotnet run --project src\backend\KudosApp.Api\KudosApp.Api.csproj
```

Then open the app URL shown in console (usually `https://localhost:5001`).

## Important Notes

- Zoho integrations are currently starter stubs (`IZohoBridge`) with clear extension points for real APIs.
- Export endpoint returns base64 artifact content (download handled in UI).
- No Node.js/npm setup is required for the UI anymore.
