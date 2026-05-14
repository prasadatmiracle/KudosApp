using System.Security.Claims;
using KudosApp.Api.Models;

namespace KudosApp.Api.Infrastructure;

public static class ControllerExtensions
{
    public static int CurrentUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("user_id");
        return int.TryParse(raw, out var id) ? id : 0;
    }

    public static AppRole CurrentRole(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<AppRole>(raw, true, out var role) ? role : AppRole.Employee;
    }
}
