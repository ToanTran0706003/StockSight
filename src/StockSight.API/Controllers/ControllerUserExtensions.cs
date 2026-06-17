using System.Security.Claims;

namespace StockSight.API.Controllers;

public static class ControllerUserExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Missing user id claim.");
    }
}
