using PayrollSaaS.Domain.Common;

namespace PayrollSaaS.Domain.Entities.Tenancy;

/// <summary>
/// Per-school configurable payroll rates. Employer PF rate defaults to 0.12 (12%),
/// ESI rate defaults to 0.0075 (0.75%). Both are decimal, not percentage.
/// SPEC-GAP: the doc never states the employer PF rate; user confirmed 12%, configurable.
/// </summary>
public class SchoolPayrollSettings : BaseEntity
{
    public Guid SchoolId { get; set; }

    /// <summary>Employer PF contribution rate (default 0.12 = 12% of PF Gross).</summary>
    public decimal EmployerPfRate { get; set; } = 0.12m;

    /// <summary>Employee ESI rate (default 0.0075 = 0.75% of PF Gross). Doc §4.</summary>
    public decimal EsiRate { get; set; } = 0.0075m;

    // Navigation
    public School School { get; set; } = null!;
}
