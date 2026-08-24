using PayrollSaaS.Domain.Common;
using PayrollSaaS.Domain.Enums;

namespace PayrollSaaS.Domain.Entities.Employees;

/// <summary>Doc §5.2. Belongs to one Division.</summary>
public class Employee : BaseEntity, IAuditable
{
    public Guid SchoolId { get; set; }
    public Guid SchoolDivisionId { get; set; }

    public string StaffName { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }

    /// <summary>Report label only — same salary rules (doc §5.2).</summary>
    public StaffType StaffType { get; set; }

    public string? BankAccountNumber { get; set; }
    public string? IfscCode { get; set; }

    /// <summary>Used to compute PF eligibility anniversary.</summary>
    public DateOnly DateOfJoining { get; set; }

    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;

    /// <summary>Employee chose to enrol in PF.</summary>
    public bool PfOptedIn { get; set; }

    public PfStatus PfStatus { get; set; } = PfStatus.NotEligible;

    /// <summary>Date PF deduction became active. Set when HR confirms eligibility.</summary>
    public DateOnly? PfActiveFrom { get; set; }

    // Navigation
    public ICollection<EmployeeSalaryComponent> SalaryComponents { get; set; } = [];
    public ICollection<EmployeePfConfig> PfConfigs { get; set; } = [];
}
