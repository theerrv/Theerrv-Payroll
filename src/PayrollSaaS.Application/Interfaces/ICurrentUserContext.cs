using PayrollSaaS.Domain.Enums;

namespace PayrollSaaS.Application.Interfaces;

/// <summary>
/// Provides the identity of the current authenticated user, sourced from JWT claims.
/// Used by EF global query filters for tenant/school scoping, and by the audit interceptor.
/// </summary>
public interface ICurrentUserContext
{
    Guid UserId { get; }
    Guid? SchoolId { get; }
    Guid? TenantId { get; }
    Guid? EmployeeId { get; }
    UserRole Role { get; }
    bool IsSuperAdmin => Role == UserRole.SuperAdmin;
}
