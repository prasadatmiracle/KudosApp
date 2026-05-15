using System.Text.Json;
using KudosApp.Api.Data;
using KudosApp.Api.DTOs;
using KudosApp.Api.Models;

namespace KudosApp.Api.Services;

public interface IDataSeeder
{
    void Seed();
}

public sealed class DataSeeder(AppDbContext db) : IDataSeeder
{
    public void Seed()
    {
        SeedBase();
        SeedDemo();
    }

    // ── Base: teams, login users, projects (idempotent) ───────────────────
    private void SeedBase()
    {
        if (db.Teams.Any()) return;

        var eng   = new Team { TeamName = "Engineering" };
        var sales = new Team { TeamName = "Sales" };
        var design = new Team { TeamName = "Design" };
        db.Teams.AddRange(eng, sales, design);
        db.SaveChanges();

        var admin   = new UserProfile { EmployeeId = "EMP-001", Name = "System Admin",   Email = "admin@kudos.local",    Role = AppRole.Admin,    TeamId = eng.TeamId };
        db.Users.Add(admin); db.SaveChanges();

        var mgr     = new UserProfile { EmployeeId = "EMP-002", Name = "Prasad (Manager)", Email = "manager@kudos.local",  Role = AppRole.Manager,  TeamId = eng.TeamId,   ManagerId = admin.UserId };
        var emp     = new UserProfile { EmployeeId = "EMP-003", Name = "Team Member",     Email = "employee@kudos.local", Role = AppRole.Employee, TeamId = eng.TeamId };
        var hr      = new UserProfile { EmployeeId = "EMP-004", Name = "HR Viewer",       Email = "hr@kudos.local",       Role = AppRole.Hr,       TeamId = sales.TeamId, ManagerId = admin.UserId };
        db.Users.AddRange(mgr, emp, hr); db.SaveChanges();

        emp.ManagerId = mgr.UserId;
        db.SaveChanges();

        var prjA = new Project { ProjectCode = "PRJ-A", ProjectName = "Core Platform",          ClientName = "Internal" };
        var prjB = new Project { ProjectCode = "PRJ-B", ProjectName = "Client Delivery",         ClientName = "Acme Corp" };
        db.Projects.AddRange(prjA, prjB); db.SaveChanges();

        db.Badges.AddRange(
            new Badge { BadgeName = "Consistent Contributor", Criteria = "20 daily updates in a month" },
            new Badge { BadgeName = "Team Player",            Criteria = "15 poll responses in a month" },
            new Badge { BadgeName = "Knowledge Sharer",       Criteria = "4 approved contributions in a month" }
        );
        db.SaveChanges();
    }

    // ── Demo: all screens get populated data (idempotent via DailyUpdates guard) ──
    private void SeedDemo()
    {
        if (db.DailyUpdates.Any()) return;  // already seeded

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // ── Resolve base entities ─────────────────────────────────────────
        var eng   = db.Teams.First(t => t.TeamName == "Engineering");
        var sales = db.Teams.First(t => t.TeamName == "Sales");
        var design = db.Teams.FirstOrDefault(t => t.TeamName == "Design")
                    ?? new Team { TeamName = "Design" };
        if (design.TeamId == 0) { db.Teams.Add(design); db.SaveChanges(); }

        var admin = db.Users.First(u => u.Email == "admin@kudos.local");
        var mgr   = db.Users.First(u => u.Email == "manager@kudos.local");
        var emp   = db.Users.First(u => u.Email == "employee@kudos.local");

        var prjA = db.Projects.First(p => p.ProjectCode == "PRJ-A");
        var prjB = db.Projects.First(p => p.ProjectCode == "PRJ-B");

        // ── Extra team members ────────────────────────────────────────────
        var alice  = new UserProfile { EmployeeId = "EMP-005", Name = "Alice Fernandes",  Email = "alice@kudos.local",  Role = AppRole.Employee, TeamId = eng.TeamId,   ManagerId = mgr.UserId };
        var bob    = new UserProfile { EmployeeId = "EMP-006", Name = "Bob Mathews",      Email = "bob@kudos.local",    Role = AppRole.Employee, TeamId = eng.TeamId,   ManagerId = mgr.UserId };
        var carol  = new UserProfile { EmployeeId = "EMP-007", Name = "Carol Sharma",     Email = "carol@kudos.local",  Role = AppRole.Employee, TeamId = sales.TeamId, ManagerId = mgr.UserId };
        var david  = new UserProfile { EmployeeId = "EMP-008", Name = "David Kurien",     Email = "david@kudos.local",  Role = AppRole.Employee, TeamId = sales.TeamId, ManagerId = mgr.UserId };
        var eve    = new UserProfile { EmployeeId = "EMP-009", Name = "Eve Thomas",       Email = "eve@kudos.local",    Role = AppRole.Employee, TeamId = design.TeamId, ManagerId = mgr.UserId };
        var frank  = new UserProfile { EmployeeId = "EMP-010", Name = "Frank D'Souza",    Email = "frank@kudos.local",  Role = AppRole.Employee, TeamId = design.TeamId, ManagerId = mgr.UserId };
        db.Users.AddRange(alice, bob, carol, david, eve, frank);
        db.SaveChanges();

        // ── More projects ─────────────────────────────────────────────────
        var prjC = new Project { ProjectCode = "PRJ-C", ProjectName = "Digital Transformation", ClientName = "TechCorp Ltd" };
        var prjD = new Project { ProjectCode = "PRJ-D", ProjectName = "Cloud Migration",         ClientName = "Global Bank" };
        var prjE = new Project { ProjectCode = "PRJ-E", ProjectName = "API Integration",          ClientName = "StartupXYZ" };
        db.Projects.AddRange(prjC, prjD, prjE);
        db.SaveChanges();

        // ── Resource allocations ──────────────────────────────────────────
        var allocs = new[]
        {
            new ResourceAllocation { UserId = mgr.UserId,   ProjectId = prjA.ProjectId, BillingType = BillingType.Billable,    StartDate = today.AddDays(-60) },
            new ResourceAllocation { UserId = emp.UserId,   ProjectId = prjB.ProjectId, BillingType = BillingType.Billable,    StartDate = today.AddDays(-60) },
            new ResourceAllocation { UserId = alice.UserId, ProjectId = prjC.ProjectId, BillingType = BillingType.Billable,    StartDate = today.AddDays(-45) },
            new ResourceAllocation { UserId = bob.UserId,   ProjectId = prjD.ProjectId, BillingType = BillingType.Billable,    StartDate = today.AddDays(-45) },
            new ResourceAllocation { UserId = carol.UserId, ProjectId = prjE.ProjectId, BillingType = BillingType.NonBillable, StartDate = today.AddDays(-30) },
            new ResourceAllocation { UserId = david.UserId, ProjectId = prjC.ProjectId, BillingType = BillingType.Shadow,      StartDate = today.AddDays(-30) },
            new ResourceAllocation { UserId = eve.UserId,   ProjectId = prjA.ProjectId, BillingType = BillingType.Trainee,     StartDate = today.AddDays(-20) },
            new ResourceAllocation { UserId = frank.UserId, ProjectId = prjB.ProjectId, BillingType = BillingType.Overhead,    StartDate = today.AddDays(-20) },
        };
        db.ResourceAllocations.AddRange(allocs);
        db.SaveChanges();

        // ── Daily updates — last 30 days ──────────────────────────────────
        // Active submitters: mgr, emp, alice, bob, carol, david
        // Lapsed (last 5 days missing): eve, frank
        var activeMembers = new[]
        {
            (mgr,   prjA, "CORE"),
            (emp,   prjB, "ACME"),
            (alice, prjC, "TECH"),
            (bob,   prjD, "BANK"),
            (carol, prjE, "STRT"),
            (david, prjC, "TECH"),
        };

        var statusCycle = new[] { DailyStatus.InProgress, DailyStatus.InProgress, DailyStatus.Completed, DailyStatus.InProgress, DailyStatus.Blocked, DailyStatus.InProgress, DailyStatus.Completed };
        var dailyUpdates = new List<DailyUpdate>();

        for (int daysBack = 30; daysBack >= 0; daysBack--)
        {
            var workDate = today.AddDays(-daysBack);
            // Skip weekends
            if (workDate.DayOfWeek == DayOfWeek.Saturday || workDate.DayOfWeek == DayOfWeek.Sunday) continue;

            foreach (var (user, project, ticketPrefix) in activeMembers)
            {
                var status = statusCycle[(daysBack + user.UserId) % statusCycle.Length];
                // Introduce some gaps (simulate ~10% miss rate)
                if ((daysBack * user.UserId) % 11 == 0 && daysBack > 3) continue;

                dailyUpdates.Add(new DailyUpdate
                {
                    UserId = user.UserId,
                    ProjectId = project.ProjectId,
                    WorkDate = workDate,
                    TicketNumber = $"{ticketPrefix}-{100 + (daysBack % 50)}",
                    Description = DailyDescriptions[(daysBack + user.UserId) % DailyDescriptions.Length],
                    Status = status,
                    CreatedAtUtc = workDate.ToDateTime(TimeOnly.Parse("09:30:00"), DateTimeKind.Utc)
                });
            }

            // Eve & Frank: only submit for days > 5
            if (daysBack > 5)
            {
                dailyUpdates.Add(new DailyUpdate { UserId = eve.UserId, ProjectId = prjA.ProjectId, WorkDate = workDate, TicketNumber = $"DSGN-{200 + daysBack}", Description = "Worked on UI mockups for the new dashboard screens.", Status = DailyStatus.InProgress, CreatedAtUtc = workDate.ToDateTime(TimeOnly.Parse("10:00:00"), DateTimeKind.Utc) });
                dailyUpdates.Add(new DailyUpdate { UserId = frank.UserId, ProjectId = prjB.ProjectId, WorkDate = workDate, TicketNumber = $"DSGN-{300 + daysBack}", Description = "Reviewed design system components and updated Figma files.", Status = DailyStatus.InProgress, CreatedAtUtc = workDate.ToDateTime(TimeOnly.Parse("10:15:00"), DateTimeKind.Utc) });
            }
        }

        // Add a couple of blocked tickets for health dashboard / nudges
        dailyUpdates.Add(new DailyUpdate { UserId = bob.UserId, ProjectId = prjD.ProjectId, WorkDate = today, TicketNumber = "BANK-205", Description = "DB migration script failing on prod — waiting for DBA access.", Status = DailyStatus.Blocked, CreatedAtUtc = DateTime.UtcNow });
        dailyUpdates.Add(new DailyUpdate { UserId = carol.UserId, ProjectId = prjE.ProjectId, WorkDate = today, TicketNumber = "STRT-190", Description = "OAuth token refresh blocked by third-party API downtime.", Status = DailyStatus.Blocked, CreatedAtUtc = DateTime.UtcNow });

        db.DailyUpdates.AddRange(dailyUpdates);
        db.SaveChanges();

        // ── Tasks / Polls ─────────────────────────────────────────────────
        var task1 = new TaskItem { Title = "Team lunch venue poll", Description = "Vote for preferred venue for our team lunch on Friday.", TaskType = TaskType.Vote, State = TaskState.Active, CreatedByUserId = mgr.UserId, DueAtUtc = DateTime.UtcNow.AddDays(2) };
        var task2 = new TaskItem { Title = "Sprint demo time preference", Description = "Which time slot works best for the sprint demo next week?", TaskType = TaskType.Vote, State = TaskState.Active, CreatedByUserId = mgr.UserId, DueAtUtc = DateTime.UtcNow.AddDays(4) };
        var task3 = new TaskItem { Title = "Update project wiki pages", Description = "Each member should update their project's Confluence page with latest status.", TaskType = TaskType.Action, State = TaskState.Active, CreatedByUserId = mgr.UserId, ProjectId = prjA.ProjectId, DueAtUtc = DateTime.UtcNow.AddDays(3) };
        var task4 = new TaskItem { Title = "New WFH policy effective Monday", Description = "Please read the updated WFH guidelines shared in the HR portal.", TaskType = TaskType.Info, State = TaskState.Active, CreatedByUserId = admin.UserId, DueAtUtc = DateTime.UtcNow.AddDays(7) };
        var task5 = new TaskItem { Title = "Q2 team outing activity vote", Description = "Choose the activity for our upcoming quarterly team outing.", TaskType = TaskType.Vote, State = TaskState.Active, CreatedByUserId = mgr.UserId, DueAtUtc = DateTime.UtcNow.AddDays(5) };
        db.Tasks.AddRange(task1, task2, task3, task4, task5);
        db.SaveChanges();

        // Some responses to tasks (so reports aren't empty)
        db.TaskResponses.AddRange(
            new TaskResponse { TaskId = task1.TaskId, UserId = emp.UserId,   Option = "Yes",   Remark = "I prefer the Italian place on MG Road." },
            new TaskResponse { TaskId = task1.TaskId, UserId = alice.UserId, Option = "Yes",   Remark = "Happy with any place near office." },
            new TaskResponse { TaskId = task1.TaskId, UserId = bob.UserId,   Option = "No",    Remark = "I'll be on leave Friday." },
            new TaskResponse { TaskId = task2.TaskId, UserId = emp.UserId,   Option = "Yes",   Remark = "Morning slot (10 AM) works for me." },
            new TaskResponse { TaskId = task2.TaskId, UserId = carol.UserId, Option = "Maybe", Remark = "Depends on client call timings." }
        );
        db.SaveChanges();

        // ── Achievements ──────────────────────────────────────────────────
        var ach1 = new Achievement { UserId = alice.UserId, Category = "Certification", Title = "AWS Solutions Architect – Associate", Description = "Cleared the SAA-C03 exam on first attempt. Scored 890/1000.", ValidationStatus = ValidationStatus.Pending, CreatedAtUtc = DateTime.UtcNow.AddDays(-6) };
        var ach2 = new Achievement { UserId = bob.UserId,   Category = "Blog",          Title = "Microservices Resilience Patterns",   Description = "Published a technical blog on retry, circuit-breaker, and bulkhead patterns for .NET services.", ValidationStatus = ValidationStatus.Pending, CreatedAtUtc = DateTime.UtcNow.AddDays(-3) };
        var ach3 = new Achievement { UserId = emp.UserId,   Category = "POC",           Title = "React 18 Concurrent Rendering POC",   Description = "Built a proof-of-concept demonstrating 40% rendering performance improvement using Concurrent Mode.", ValidationStatus = ValidationStatus.Approved, ValidatedByUserId = mgr.UserId, ValidatedAtUtc = DateTime.UtcNow.AddDays(-10) };
        var ach4 = new Achievement { UserId = carol.UserId, Category = "Appreciation",  Title = "Client Appreciation – Acme Corp",     Description = "Received written appreciation from the Acme Corp delivery head for seamless UAT support.", ValidationStatus = ValidationStatus.Approved, ValidatedByUserId = mgr.UserId, ValidatedAtUtc = DateTime.UtcNow.AddDays(-8) };
        var ach5 = new Achievement { UserId = david.UserId, Category = "Certification", Title = "Azure Developer Associate (AZ-204)",  Description = "Passed AZ-204 certification. Strengthens our Azure capability for upcoming cloud projects.", ValidationStatus = ValidationStatus.Pending, CreatedAtUtc = DateTime.UtcNow.AddDays(-7) };
        var ach6 = new Achievement { UserId = alice.UserId, Category = "Blog",          Title = "Docker Deep Dive: From Dev to Prod",  Description = "Comprehensive guide on containerising .NET apps and deploying to AKS. 500+ reads in first week.", ValidationStatus = ValidationStatus.Approved, ValidatedByUserId = mgr.UserId, ValidatedAtUtc = DateTime.UtcNow.AddDays(-15) };
        var ach7 = new Achievement { UserId = emp.UserId,   Category = "Training",      Title = "TypeScript Advanced Patterns",        Description = "Completed Udemy advanced TypeScript course (24 hrs). Certificate attached.", ValidationStatus = ValidationStatus.Approved, ValidatedByUserId = mgr.UserId, ValidatedAtUtc = DateTime.UtcNow.AddDays(-20) };
        var ach8 = new Achievement { UserId = frank.UserId, Category = "POC",           Title = "Figma Auto-Layout Migration",         Description = "Migrated design system to Figma auto-layout — later found approach incompatible with current toolchain.", ValidationStatus = ValidationStatus.Rejected, ValidatedByUserId = mgr.UserId, ValidatedAtUtc = DateTime.UtcNow.AddDays(-5) };
        db.Achievements.AddRange(ach1, ach2, ach3, ach4, ach5, ach6, ach7, ach8);
        db.SaveChanges();

        // Validation records for pending achievements
        db.Validations.AddRange(
            new ValidationRecord { EntityType = ValidationEntityType.Achievement, EntityId = ach1.AchievementId, Status = ValidationStatus.Pending, CreatedAtUtc = ach1.CreatedAtUtc },
            new ValidationRecord { EntityType = ValidationEntityType.Achievement, EntityId = ach2.AchievementId, Status = ValidationStatus.Pending, CreatedAtUtc = ach2.CreatedAtUtc },
            new ValidationRecord { EntityType = ValidationEntityType.Achievement, EntityId = ach5.AchievementId, Status = ValidationStatus.Pending, CreatedAtUtc = ach5.CreatedAtUtc }
        );
        db.SaveChanges();

        // ── Sales Enquiries ───────────────────────────────────────────────
        var enq1 = new SalesEnquiry { ClientName = "TechCorp Ltd",    Requirement = "Senior Java Developer",     Technology = "Java / Spring Boot",  EnquiryDate = today.AddDays(-12), SalesCoordinator = carol.Name, Status = "Open",         CreatedByUserId = carol.UserId, ValidationStatus = ValidationStatus.Pending };
        var enq2 = new SalesEnquiry { ClientName = "Global Bank",     Requirement = ".NET Solution Architect",   Technology = ".NET / Azure",        EnquiryDate = today.AddDays(-9),  SalesCoordinator = carol.Name, Status = "InDiscussion", CreatedByUserId = carol.UserId, ValidationStatus = ValidationStatus.Pending };
        var enq3 = new SalesEnquiry { ClientName = "StartupXYZ",      Requirement = "React Frontend Developer",  Technology = "React / TypeScript",  EnquiryDate = today.AddDays(-10), SalesCoordinator = david.Name, Status = "Open",         CreatedByUserId = david.UserId, ValidationStatus = ValidationStatus.Pending };
        var enq4 = new SalesEnquiry { ClientName = "Innovate Inc",    Requirement = "Python Data Engineer",      Technology = "Python / Spark",      EnquiryDate = today.AddDays(-20), SalesCoordinator = david.Name, Status = "Shortlisted",  CreatedByUserId = david.UserId, ValidationStatus = ValidationStatus.Approved, ValidatedByUserId = mgr.UserId, ValidatedAtUtc = DateTime.UtcNow.AddDays(-5) };
        var enq5 = new SalesEnquiry { ClientName = "MegaCorp Systems", Requirement = "Cloud DevOps Engineer",    Technology = "AWS / Terraform",     EnquiryDate = today.AddDays(-8),  SalesCoordinator = carol.Name, Status = "Open",         CreatedByUserId = carol.UserId, ValidationStatus = ValidationStatus.Pending };
        var enq6 = new SalesEnquiry { ClientName = "DataViz Co",      Requirement = "Full Stack Developer",      Technology = "Angular / .NET",      EnquiryDate = today.AddDays(-25), SalesCoordinator = david.Name, Status = "Won",          CreatedByUserId = david.UserId, ValidationStatus = ValidationStatus.Approved, ValidatedByUserId = mgr.UserId, ValidatedAtUtc = DateTime.UtcNow.AddDays(-10) };
        db.SalesEnquiries.AddRange(enq1, enq2, enq3, enq4, enq5, enq6);
        db.SaveChanges();

        // Validation records for pending sales enquiries
        db.Validations.AddRange(
            new ValidationRecord { EntityType = ValidationEntityType.SalesEnquiry, EntityId = enq1.SalesEnquiryId, Status = ValidationStatus.Pending, CreatedAtUtc = enq1.CreatedAtUtc },
            new ValidationRecord { EntityType = ValidationEntityType.SalesEnquiry, EntityId = enq2.SalesEnquiryId, Status = ValidationStatus.Pending, CreatedAtUtc = enq2.CreatedAtUtc },
            new ValidationRecord { EntityType = ValidationEntityType.SalesEnquiry, EntityId = enq3.SalesEnquiryId, Status = ValidationStatus.Pending, CreatedAtUtc = enq3.CreatedAtUtc },
            new ValidationRecord { EntityType = ValidationEntityType.SalesEnquiry, EntityId = enq5.SalesEnquiryId, Status = ValidationStatus.Pending, CreatedAtUtc = enq5.CreatedAtUtc }
        );
        db.SaveChanges();

        // ── Engagements ───────────────────────────────────────────────────
        db.Engagements.AddRange(
            new Engagement { ClientName = "TechCorp Ltd",  ProjectName = "ERP Modernisation",    NumberOfPositions = 3, Details = "Java + Spring Boot micro-services team for 6-month engagement.", CreatedByUserId = carol.UserId },
            new Engagement { ClientName = "Global Bank",   ProjectName = "Core Banking API Layer", NumberOfPositions = 2, Details = ".NET architect + senior developer for API-first transformation.", CreatedByUserId = david.UserId }
        );

        // ── Sales Sessions ────────────────────────────────────────────────
        db.SalesSessions.AddRange(
            new SalesSession { Title = "Azure Pitch Deck Workshop", SessionDate = today.AddDays(-14), TeamId = eng.TeamId, Description = "Internal workshop to sharpen Azure pitch for banking clients. Covered cost-optimisation and HA patterns.", CreatedByUserId = mgr.UserId },
            new SalesSession { Title = "Client Demo Best Practices", SessionDate = today.AddDays(-7),  TeamId = sales.TeamId, Description = "How to run a compelling product demo: prep, story, live coding, Q&A handling.", CreatedByUserId = admin.UserId }
        );
        db.SaveChanges();

        // ── Events ────────────────────────────────────────────────────────
        var evt1 = new EventItem { Title = "Q1 Team Outing – Adventure Park",  Description = "Team bonding day at Wonderla. Activities included rope course, kayaking, and group challenges.", EventDate = today.AddDays(-21), Location = "Wonderla, Bangalore",  CreatedByUserId = mgr.UserId };
        var evt2 = new EventItem { Title = "Tech Talk: Azure Cost Optimisation", Description = "30-minute internal tech talk covering Reserved Instances, Spot VMs, and tagging strategies for cost visibility.", EventDate = today.AddDays(-10), Location = "Conference Room A2", CreatedByUserId = mgr.UserId };
        var evt3 = new EventItem { Title = "New Joiner Welcome Lunch",           Description = "Lunch to welcome Eve Thomas and Frank D'Souza to the team.", EventDate = today.AddDays(3), Location = "Mainland China, Koramangala", CreatedByUserId = mgr.UserId };
        db.Events.AddRange(evt1, evt2, evt3);
        db.SaveChanges();

        db.EventMedia.AddRange(
            new EventMedia { EventId = evt1.EventId, WorkDriveFileUrl = "https://workdrive.zoho.com/file/demo-outing-photo1.jpg", UploadedByUserId = mgr.UserId },
            new EventMedia { EventId = evt1.EventId, WorkDriveFileUrl = "https://workdrive.zoho.com/file/demo-outing-photo2.jpg", UploadedByUserId = alice.UserId },
            new EventMedia { EventId = evt2.EventId, WorkDriveFileUrl = "https://workdrive.zoho.com/file/demo-techtalk-slides.pdf", UploadedByUserId = mgr.UserId }
        );
        db.SaveChanges();

        // ── Meetings + MOM ────────────────────────────────────────────────
        var meet1 = new MeetingRecord { Title = "Weekly Sprint Review – Sprint 23", MeetingAtUtc = DateTime.UtcNow.AddDays(-7), ZohoMeetingUrl = "https://meeting.zoho.com/demo/sprint23", CreatedByUserId = mgr.UserId };
        var meet2 = new MeetingRecord { Title = "Client Sync – Acme Corp",          MeetingAtUtc = DateTime.UtcNow.AddDays(-4), ZohoMeetingUrl = "https://meeting.zoho.com/demo/acmesync",  CreatedByUserId = mgr.UserId };
        db.Meetings.AddRange(meet1, meet2);
        db.SaveChanges();

        db.MomEntries.AddRange(
            new MomEntry { MeetingId = meet1.MeetingId, Summary = "Sprint 23 completed with 92% velocity. Two tickets moved to next sprint due to scope change. Team agreed to revisit estimation process.", ActionItems = "Bob: fix DB migration by Wed; Alice: complete API docs by Thu; Prasad: send velocity report to stakeholders", CreatedByUserId = mgr.UserId },
            new MomEntry { MeetingId = meet2.MeetingId, Summary = "Acme Corp happy with UAT progress. Requested two new reports by end of month. Agreed to weekly sync calls going forward.", ActionItems = "Employee: build summary report; Carol: schedule next sync call; Prasad: share timeline document", CreatedByUserId = mgr.UserId }
        );
        db.SaveChanges();

        // ── Action Items ──────────────────────────────────────────────────
        db.ActionItems.AddRange(
            new ActionItem { Title = "Fix login redirect bug on mobile",    Description = "Users on iOS Safari get redirect loop after session timeout.", AssignedToUserId = emp.UserId,   CreatedByUserId = mgr.UserId,  DueDate = today.AddDays(2),  Priority = ActionItemPriority.High,     Status = ActionItemStatus.InProgress },
            new ActionItem { Title = "Write unit tests for UserService",     Description = "Coverage currently at 42%. Target 80% for next release.", AssignedToUserId = alice.UserId, CreatedByUserId = mgr.UserId,  DueDate = today.AddDays(5),  Priority = ActionItemPriority.Medium,   Status = ActionItemStatus.Open },
            new ActionItem { Title = "Prepare Q2 client presentation deck", Description = "PowerPoint covering deliverables, risks, and roadmap for Q2.", AssignedToUserId = mgr.UserId,  CreatedByUserId = admin.UserId, DueDate = today.AddDays(7),  Priority = ActionItemPriority.High,     Status = ActionItemStatus.Open },
            new ActionItem { Title = "Update CI/CD deployment scripts",     Description = "Scripts need updating for the new AKS cluster endpoint.", AssignedToUserId = bob.UserId,   CreatedByUserId = mgr.UserId,  DueDate = today.AddDays(-1), Priority = ActionItemPriority.Critical, Status = ActionItemStatus.Open }  // overdue — for nudge
        );
        db.SaveChanges();

        // ── Points ────────────────────────────────────────────────────────
        var pointsData = new[]
        {
            (mgr.UserId,   185), (emp.UserId,   150), (alice.UserId, 170),
            (bob.UserId,   130), (carol.UserId, 125), (david.UserId, 115),
            (eve.UserId,    90), (frank.UserId,  75),
        };
        foreach (var (userId, pts) in pointsData)
        {
            db.Points.Add(new PointsLog { UserId = userId, Points = pts, ActivityType = "SeededDemo", CreatedAtUtc = DateTime.UtcNow.AddDays(-1) });
        }
        db.SaveChanges();

        // ── Badges assigned ───────────────────────────────────────────────
        var badges = db.Badges.ToList();
        var consistentBadge = badges.FirstOrDefault(b => b.BadgeName == "Consistent Contributor");
        var teamPlayerBadge = badges.FirstOrDefault(b => b.BadgeName == "Team Player");
        var knowledgeBadge  = badges.FirstOrDefault(b => b.BadgeName == "Knowledge Sharer");

        if (consistentBadge != null)
        {
            db.UserBadges.AddRange(
                new UserBadge { UserId = alice.UserId, BadgeId = consistentBadge.BadgeId, AwardedAtUtc = DateTime.UtcNow.AddDays(-5) },
                new UserBadge { UserId = emp.UserId,   BadgeId = consistentBadge.BadgeId, AwardedAtUtc = DateTime.UtcNow.AddDays(-5) }
            );
        }
        if (teamPlayerBadge != null)
        {
            db.UserBadges.AddRange(
                new UserBadge { UserId = mgr.UserId, BadgeId = teamPlayerBadge.BadgeId, AwardedAtUtc = DateTime.UtcNow.AddDays(-10) },
                new UserBadge { UserId = bob.UserId, BadgeId = teamPlayerBadge.BadgeId, AwardedAtUtc = DateTime.UtcNow.AddDays(-10) }
            );
        }
        if (knowledgeBadge != null)
        {
            db.UserBadges.Add(new UserBadge { UserId = alice.UserId, BadgeId = knowledgeBadge.BadgeId, AwardedAtUtc = DateTime.UtcNow.AddDays(-3) });
        }
        db.SaveChanges();

        // ── Reports (pre-built) ───────────────────────────────────────────
        var weekStart = today.AddDays(-(int)today.DayOfWeek - 6);   // last Mon
        var weekEnd   = weekStart.AddDays(4);                        // last Fri

        var weeklyPayload = new WeeklyReportPayload
        {
            ManagerNotes = "Strong week overall — 3 certifications in progress, Acme UAT going smoothly. Blocked tickets need DBA follow-up.",
            Tickets = dailyUpdates
                .Where(d => d.WorkDate >= weekStart && d.WorkDate <= weekEnd)
                .GroupBy(d => new { d.TicketNumber, d.UserId })
                .Select(g =>
                {
                    var first = g.First();
                    var user  = db.Users.Find(first.UserId);
                    var proj  = db.Projects.Find(first.ProjectId);
                    return new WeeklyTicketRow
                    {
                        TicketNumber = first.TicketNumber,
                        ProjectName  = proj?.ProjectName ?? "Unknown",
                        Description  = first.Description,
                        OwnerName    = user?.Name ?? "Unknown",
                        Status       = first.Status,
                        WorkDate     = first.WorkDate
                    };
                }).Take(30).ToList()
        };

        var monthlyPayload = new MonthlyReportSection
        {
            ResourceUtilization = new Dictionary<string, int>
            {
                ["Billable"] = 4, ["NonBillable"] = 1, ["Shadow"] = 1, ["Trainee"] = 1, ["Overhead"] = 1
            },
            Engagements          = db.Engagements.ToList(),
            ApprovedAchievements = db.Achievements.Where(a => a.ValidationStatus == ValidationStatus.Approved).ToList(),
            ApprovedSalesEnquiries = db.SalesEnquiries.Where(e => e.ValidationStatus == ValidationStatus.Approved).ToList(),
            SalesSessions        = db.SalesSessions.ToList(),
            Events               = db.Events.ToList()
        };

        var qtrPayload = new QuarterlyReportSection
        {
            Year    = today.Year,
            Quarter = (today.Month - 1) / 3 + 1,
            EnquiryCountByMonth     = new Dictionary<string, int> { ["Jan"] = 4, ["Feb"] = 6, ["Mar"] = 3 },
            AchievementCountByMonth = new Dictionary<string, int> { ["Jan"] = 3, ["Feb"] = 5, ["Mar"] = 4 },
            ParticipationByMonth    = new Dictionary<string, int> { ["Jan"] = 180, ["Feb"] = 195, ["Mar"] = 210 }
        };

        db.Reports.AddRange(
            new ReportRecord
            {
                ReportType        = ReportType.Weekly,
                StartDate         = weekStart,
                EndDate           = weekEnd,
                PayloadJson       = JsonSerializer.Serialize(weeklyPayload),
                Status            = ReportStatus.Locked,
                GeneratedByUserId = mgr.UserId,
                CreatedAtUtc      = DateTime.UtcNow.AddDays(-3),
                UpdatedAtUtc      = DateTime.UtcNow.AddDays(-2)
            },
            new ReportRecord
            {
                ReportType        = ReportType.Monthly,
                StartDate         = new DateOnly(today.Year, today.Month, 1).AddMonths(-1),
                EndDate           = new DateOnly(today.Year, today.Month, 1).AddDays(-1),
                PayloadJson       = JsonSerializer.Serialize(monthlyPayload),
                Status            = ReportStatus.Locked,
                GeneratedByUserId = mgr.UserId,
                CreatedAtUtc      = DateTime.UtcNow.AddDays(-10),
                UpdatedAtUtc      = DateTime.UtcNow.AddDays(-8)
            },
            new ReportRecord
            {
                ReportType        = ReportType.Quarterly,
                StartDate         = new DateOnly(today.Year, (((today.Month - 1) / 3) * 3) + 1, 1),
                EndDate           = today,
                PayloadJson       = JsonSerializer.Serialize(qtrPayload),
                Status            = ReportStatus.Draft,
                GeneratedByUserId = mgr.UserId,
                CreatedAtUtc      = DateTime.UtcNow.AddDays(-1),
                UpdatedAtUtc      = DateTime.UtcNow.AddDays(-1)
            }
        );
        db.SaveChanges();

        // ── Inbox Tasks ───────────────────────────────────────────────────
        db.InboxTasks.AddRange(
            new InboxTask { UserId = emp.UserId, SourceChannel = "ZohoMail", SourceSender = "teamlead@acme.com", SourceMessageId = "msg-001", SourcePreview = "Hi, can you please review PR #342 for the new auth module? Need it merged before end of day.", ExtractedTaskText = "Review PR #342 for the new auth module", DeduplicationHash = "demo-hash-001", IsPrivate = true, Category = InboxTaskCategory.Development, Priority = InboxTaskPriority.High, State = InboxTaskState.PendingConfirmation, DueAtUtc = DateTime.UtcNow.AddDays(1) },
            new InboxTask { UserId = emp.UserId, SourceChannel = "ZohoCliq", SourceSender = "manager@kudos.local", SourceMessageId = "msg-002", SourcePreview = "Please update the API documentation for the new endpoints before end of this week.", ExtractedTaskText = "Update API documentation for new endpoints before end of week", DeduplicationHash = "demo-hash-002", IsPrivate = false, Category = InboxTaskCategory.Development, Priority = InboxTaskPriority.Medium, State = InboxTaskState.Active, DueAtUtc = DateTime.UtcNow.AddDays(4) },
            new InboxTask { UserId = emp.UserId, SourceChannel = "ZohoMail", SourceSender = "carol@kudos.local", SourceMessageId = "msg-003", SourcePreview = "Could you send the status update to the Acme Corp client today? They are following up.", ExtractedTaskText = "Send status update email to Acme Corp client", DeduplicationHash = "demo-hash-003", IsPrivate = false, Category = InboxTaskCategory.Communicate, Priority = InboxTaskPriority.High, State = InboxTaskState.InProgress, DueAtUtc = DateTime.UtcNow.AddDays(0) },
            new InboxTask { UserId = mgr.UserId,  SourceChannel = "ZohoMail", SourceSender = "admin@kudos.local", SourceMessageId = "msg-004", SourcePreview = "Reminder: Q2 resource planning spreadsheet needs to be submitted by Friday.", ExtractedTaskText = "Submit Q2 resource planning spreadsheet by Friday", DeduplicationHash = "demo-hash-004", IsPrivate = true, Category = InboxTaskCategory.ReportGeneration, Priority = InboxTaskPriority.Critical, State = InboxTaskState.PendingConfirmation, DueAtUtc = DateTime.UtcNow.AddDays(3) }
        );
        db.SaveChanges();
    }

    private static readonly string[] DailyDescriptions =
    [
        "Worked on implementing the new authentication middleware and writing unit tests.",
        "Reviewed pull requests and provided feedback on the API design changes.",
        "Fixed performance issues in the data pipeline — reduced query time by 35%.",
        "Participated in client call, updated user stories based on feedback.",
        "Integrated third-party payment gateway and handled edge cases.",
        "Refactored legacy service layer to use dependency injection correctly.",
        "Updated CI/CD pipeline to add security scanning step.",
        "Worked on dashboard charts — wired live data from the reporting API.",
        "Pair-programmed with teammate to debug intermittent session timeout bug.",
        "Completed code review backlog, merged 4 PRs after addressing comments.",
        "Wrote end-to-end test coverage for the onboarding flow.",
        "Analysed and resolved memory leak in background job scheduler.",
        "Set up local Kubernetes environment for integration testing.",
        "Documented API endpoints using Swagger annotations.",
        "Assisted QA team with test data setup and environment issues.",
    ];
}
