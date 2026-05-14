using KudosApp.Api.Data;
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
        if (db.Teams.Any()) return;

        var engTeam = new Team { TeamName = "Engineering" };
        var salesTeam = new Team { TeamName = "Sales" };
        db.Teams.AddRange(engTeam, salesTeam);
        db.SaveChanges();

        var admin = new UserProfile { EmployeeId = "EMP-0001", Name = "System Admin", Email = "admin@kudos.local", Role = AppRole.Admin, TeamId = engTeam.TeamId };
        db.Users.Add(admin);
        db.SaveChanges();

        var manager = new UserProfile { EmployeeId = "EMP-0002", Name = "Team Manager", Email = "manager@kudos.local", Role = AppRole.Manager, ManagerId = admin.UserId, TeamId = engTeam.TeamId };
        var employee = new UserProfile { EmployeeId = "EMP-0003", Name = "Team Member", Email = "employee@kudos.local", Role = AppRole.Employee, TeamId = engTeam.TeamId };
        var hr = new UserProfile { EmployeeId = "EMP-0004", Name = "HR Viewer", Email = "hr@kudos.local", Role = AppRole.Hr, ManagerId = admin.UserId, TeamId = salesTeam.TeamId };
        db.Users.AddRange(manager, employee, hr);
        db.SaveChanges();

        employee.ManagerId = manager.UserId;
        db.SaveChanges();

        var prjA = new Project { ProjectCode = "PRJ-A", ProjectName = "Core Platform", ClientName = "Internal" };
        var prjB = new Project { ProjectCode = "PRJ-B", ProjectName = "Client Delivery", ClientName = "Acme Corp" };
        db.Projects.AddRange(prjA, prjB);
        db.SaveChanges();

        db.ResourceAllocations.AddRange(
            new ResourceAllocation { UserId = manager.UserId, ProjectId = prjA.ProjectId, BillingType = BillingType.Billable, StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(-1)) },
            new ResourceAllocation { UserId = employee.UserId, ProjectId = prjB.ProjectId, BillingType = BillingType.Billable, StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(-1)) }
        );

        db.Badges.AddRange(
            new Badge { BadgeName = "Consistent Contributor", Criteria = "20 daily updates in month" },
            new Badge { BadgeName = "Team Player", Criteria = "15 poll responses in month" },
            new Badge { BadgeName = "Knowledge Sharer", Criteria = "4 approved knowledge contributions in month" }
        );

        db.SaveChanges();
    }
}
