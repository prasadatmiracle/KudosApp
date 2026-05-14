using KudosApp.Api.Infrastructure;
using KudosApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KudosApp.Api.Controllers;

[ApiController]
[Route("api/nudges")]
[Authorize]
public sealed class NudgesController(ISmartNudgeService nudgeService) : ControllerBase
{
    /// <summary>
    /// Returns current nudge items for the authenticated manager's visible team.
    /// Groups results into stale enquiries, blocked ticket streaks, and overdue pending achievements.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult GetNudges()
    {
        var managerId = User.CurrentUserId();
        var summary = nudgeService.GetNudges(managerId);
        return Ok(new
        {
            totalCount            = summary.TotalCount,
            staleEnquiries        = summary.StaleEnquiries,
            blockedTickets        = summary.BlockedTickets,
            pendingAchievements   = summary.PendingAchievements
        });
    }

    /// <summary>
    /// Returns nudge counts only — lightweight call for nav badge.
    /// </summary>
    [HttpGet("counts")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult GetNudgeCounts()
    {
        var managerId = User.CurrentUserId();
        var summary   = nudgeService.GetNudges(managerId);
        return Ok(new
        {
            total               = summary.TotalCount,
            staleEnquiries      = summary.StaleEnquiries.Count,
            blockedTickets      = summary.BlockedTickets.Count,
            pendingAchievements = summary.PendingAchievements.Count
        });
    }
}
