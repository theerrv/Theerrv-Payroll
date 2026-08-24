using PayrollSaaS.Domain.Common;

namespace PayrollSaaS.Domain.Entities.Employees;

/// <summary>
/// PF Gross override (doc §5.2). Stores the explicit PF Gross amount (e.g. ₹14,000 = Basic).
/// If absent, system falls back to the basic component amount. Versioned with effective_from.
/// </summary>
public class EmployeePfConfig : BaseEntity, IAuditable
{
    public Guid EmployeeId { get; set; }

    /// <summary>The PF Gross amount. Typically equals Basic component.</summary>
    public decimal PfGross { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    // Navigation
    public Employee Employee { get; set; } = null!;
}
