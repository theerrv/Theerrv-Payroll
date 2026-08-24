using PayrollSaaS.Domain.Common;
using PayrollSaaS.Domain.Enums;

namespace PayrollSaaS.Domain.Entities.Advances;

/// <summary>Doc §5.4. Employee advance with installment-based auto-recovery.</summary>
public class Advance : BaseEntity, IAuditable
{
    public Guid EmployeeId { get; set; }
    public Guid SchoolId { get; set; }

    public decimal TotalAmount { get; set; }
    public string? Reason { get; set; }
    public DateOnly GivenDate { get; set; }

    /// <summary>First day of the first recovery month.</summary>
    public DateOnly RecoveryStartMonth { get; set; }

    public decimal InstallmentAmount { get; set; }
    public int TotalInstallments { get; set; }

    /// <summary>Running total — updated at payroll finalize only (SPEC-GAP 6).</summary>
    public int InstallmentsRecovered { get; set; }

    /// <summary>Running balance — updated at payroll finalize only.</summary>
    public decimal BalanceAmount { get; set; }

    public AdvanceStatus Status { get; set; } = AdvanceStatus.Active;

    // Navigation
    public ICollection<AdvanceInstallment> Installments { get; set; } = [];
}
