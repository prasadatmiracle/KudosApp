using System.Text.Json;
using KudosApp.Api.Data;
using KudosApp.Api.DTOs;
using KudosApp.Api.Infrastructure;
using KudosApp.Api.Models;
using KudosApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KudosApp.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public sealed class TasksController(
    AppDbContext db,
    IAuditService auditService,
    IPointsService pointsService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<TaskItem> Create(CreateTaskInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) return BadRequest("Title is required.");

        var userId = User.CurrentUserId();
        var created = new TaskItem
        {
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            TaskType = input.TaskType,
            State = TaskState.Active,
            CreatedByUserId = userId,
            ProjectId = input.ProjectId,
            DueAtUtc = input.DueAtUtc == default ? DateTime.UtcNow.AddDays(1) : input.DueAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Tasks.Add(created);
        db.SaveChanges();

        auditService.Write(userId, "CREATE_TASK", nameof(TaskItem), created.TaskId, JsonSerializer.Serialize(created));
        return Ok(created);
    }

    [HttpGet("active")]
    public ActionResult<IReadOnlyCollection<TaskItem>> Active()
    {
        var rows = db.Tasks.Where(x => x.State == TaskState.Active).OrderBy(x => x.DueAtUtc).ToList();
        return Ok(rows);
    }

    [HttpPost("{taskId:int}/respond")]
    public ActionResult Respond(int taskId, TaskResponseInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Option)) return BadRequest("Option is required.");

        var userId = User.CurrentUserId();
        var task = db.Tasks.SingleOrDefault(x => x.TaskId == taskId);
        if (task is null) return NotFound("Task not found.");

        var existing = db.TaskResponses.SingleOrDefault(x => x.TaskId == taskId && x.UserId == userId);
        TaskResponse response;
        if (existing is not null)
        {
            existing.Option = input.Option.Trim();
            existing.Remark = input.Remark?.Trim();
            existing.CreatedAtUtc = DateTime.UtcNow;
            db.SaveChanges();
            response = existing;
        }
        else
        {
            response = new TaskResponse
            {
                TaskId = taskId,
                UserId = userId,
                Option = input.Option.Trim(),
                Remark = input.Remark?.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };
            db.TaskResponses.Add(response);
            db.SaveChanges();
            pointsService.AddPoints(userId, 2, "TaskResponse", response.TaskResponseId);
        }

        auditService.Write(userId, "RESPOND_TASK", nameof(TaskItem), taskId, JsonSerializer.Serialize(response));
        return Ok();
    }

    [HttpGet("{taskId:int}/report")]
    public ActionResult<IReadOnlyCollection<TaskResponseExportRow>> Report(int taskId)
    {
        if (!db.Tasks.Any(x => x.TaskId == taskId)) return NotFound("Task not found.");

        var rows = db.TaskResponses
            .Where(x => x.TaskId == taskId)
            .Join(db.Users, r => r.UserId, u => u.UserId, (r, u) => new TaskResponseExportRow
            {
                Name = u.Name,
                Option = r.Option,
                Remark = r.Remark,
                CreatedAtUtc = r.CreatedAtUtc
            })
            .OrderBy(x => x.Name)
            .ToList();
        return Ok(rows);
    }
}
