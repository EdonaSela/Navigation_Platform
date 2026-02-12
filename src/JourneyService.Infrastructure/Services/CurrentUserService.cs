using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst("oid")?.Value
                          ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public bool IsAdmin
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return false;
            }

            if (user.IsInRole("Admin"))
            {
                return true;
            }

            if (user.Claims.Any(c =>
                    c.Type == "roles" &&
                    string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return user.Claims
                .Where(c => c.Type == "scope" || c.Type == "scp")
                .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Any(scope => string.Equals(scope, "Admin", StringComparison.OrdinalIgnoreCase));
        }
    }
}
