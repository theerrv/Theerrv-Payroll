using PayrollSaaS.Domain.Common;
using PayrollSaaS.Domain.Enums;

namespace PayrollSaaS.Domain.Entities.Employees;

/// <summary>
/// Versioned salary component (doc §5.2). effective_to = null means currently active.
/// salary_after_pf = SUM of active earning components on a given date.
/// </summary>
public class EmployeeSalaryComponent : BaseEntity, IAuditable
{
    public Guid EmployeeId { get; set; }
    public ComponentName ComponentName { get; set; }
    public ComponentType ComponentType { get; set; }

    /// <summary>Monetary amount — stored as decimal(18,4).</summary>
    public decimal Amount { get; set; }

    /// <summary>First day this component is effective.</summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>Last day (inclusive). NULL = currently active.</summary>
    public DateOnly? EffectiveTo { get; set; }

    // Navigation
    public Employee Employee { get; set; } = null!;
}
