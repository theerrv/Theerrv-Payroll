using PayrollSaaS.Domain.Common;
using PayrollSaaS.Domain.Enums;

namespace PayrollSaaS.Domain.Entities.Auth;

/// <summary>Doc §5.6. school_id is null for super_admin.</summary>
public class User : BaseEntity
{
    public Guid? SchoolId { get; set; }

    /// <summary>For self-service: links User to their Employee record.</summary>
    public Guid? EmployeeId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}
