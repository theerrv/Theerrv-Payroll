using PayrollSaaS.Domain.Common;

namespace PayrollSaaS.Domain.Entities.Tenancy;

/// <summary>The client organisation (doc §5.1). Belongs to a Tenant.</summary>
public class School : BaseEntity, IAuditable
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<SchoolDivision> Divisions { get; set; } = [];
    public SchoolPayrollSettings? PayrollSettings { get; set; }
}
