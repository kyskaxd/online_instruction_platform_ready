using System.Security.Claims;

namespace InstructionPlatform.Api.Services;

public static class CurrentUserService
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var rawId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawId, out var id) ? id : 0;
    }

    public static int? GetEmployeeId(this ClaimsPrincipal user)
    {
        var rawId = user.FindFirstValue("employeeId");
        return int.TryParse(rawId, out var id) ? id : null;
    }
}
