using PayrollSaaS.Domain.Common;

namespace PayrollSaaS.Domain.Entities.Tenancy;

/// <summary>
/// Configurable sub-entity of a School (doc §5.1). e.g. "Matric", "ICSE".
/// Payroll runs are scoped per division.
/// </summary>
public class SchoolDivision : BaseEntity, IAuditable
{
    public Guid SchoolId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public School School { get; set; } = null!;
}
