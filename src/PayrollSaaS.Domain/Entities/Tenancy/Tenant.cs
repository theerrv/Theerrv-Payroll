using PayrollSaaS.Domain.Common;

namespace PayrollSaaS.Domain.Entities.Tenancy;

/// <summary>
/// Top-level SaaS account owner (doc §5.1). Enables multi-tenant billing later.
/// Currently one tenant per deployment; the schema supports N.
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Plan { get; set; } = "free";
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<School> Schools { get; set; } = [];
}
