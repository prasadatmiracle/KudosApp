using KudosApp.Api.Data;
using KudosApp.Api.Models;

namespace KudosApp.Api.Services;

public interface IAuditService
{
    void Write(int actorUserId, string action, string entityType, int entityId, string metadataJson);
}

public sealed class AuditService(AppDbContext db) : IAuditService
{
    public void Write(int actorUserId, string action, string entityType, int entityId, string metadataJson)
    {
        db.AuditEntries.Add(new AuditEntry
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            MetadataJson = metadataJson,
            CreatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}

public interface IReminderPolicy
{
    bool CanDispatchReminder(int userId, DateOnly date);
    void MarkReminderSent(int userId, DateOnly date);
}

public sealed class ReminderPolicy(AppDbContext db) : IReminderPolicy
{
    private const int DailyReminderLimit = 2;

    public bool CanDispatchReminder(int userId, DateOnly date)
    {
        var row = db.ReminderDispatches.SingleOrDefault(x => x.UserId == userId && x.DispatchDate == date);
        return row is null || row.Count < DailyReminderLimit;
    }

    public void MarkReminderSent(int userId, DateOnly date)
    {
        var row = db.ReminderDispatches.SingleOrDefault(x => x.UserId == userId && x.DispatchDate == date);
        if (row is null)
        {
            db.ReminderDispatches.Add(new ReminderDispatch
            {
                UserId = userId,
                DispatchDate = date,
                Count = 1,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            row.Count += 1;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }
        db.SaveChanges();
    }
}
