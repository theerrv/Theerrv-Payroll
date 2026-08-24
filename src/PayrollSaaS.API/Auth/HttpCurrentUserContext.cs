using System.Security.Claims;
using PayrollSaaS.Application.Interfaces;
using PayrollSaaS.Domain.Enums;

namespace PayrollSaaS.API.Auth;

/// <summary>
/// Reads the current user identity from JWT claims on the HttpContext.
/// </summary>
public class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId => Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
    public Guid? SchoolId => Guid.TryParse(User?.FindFirstValue("school_id"), out var id) ? id : null;
    public Guid? TenantId => Guid.TryParse(User?.FindFirstValue("tenant_id"), out var id) ? id : null;
    public Guid? EmployeeId => Guid.TryParse(User?.FindFirstValue("employee_id"), out var id) ? id : null;
    public UserRole Role => Enum.TryParse<UserRole>(User?.FindFirstValue(ClaimTypes.Role), true, out var r) ? r : UserRole.Employee;
}
