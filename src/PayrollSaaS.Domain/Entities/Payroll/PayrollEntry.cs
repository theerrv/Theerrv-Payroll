using PayrollSaaS.Domain.Common;

namespace PayrollSaaS.Domain.Entities.Payroll;

/// <summary>
/// One row per employee per run (doc §5.5). Contains both snapshots (immutable after finalize)
/// and calculated fields.
/// </summary>
public class PayrollEntry : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }

    // ── Snapshots (immutable after finalize) ──
    public decimal SalaryAfterPf { get; set; }
    public decimal PfGross { get; set; }
    public bool IsPfEligible { get; set; }

    // ── Calculated fields ──
    public int LopDays { get; set; }
    public decimal LopDeduction { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalAdditions { get; set; }
    public decimal NettPay { get; set; }
    public decimal EsiAmount { get; set; }
    public decimal NettSalary { get; set; }

    // ── HR verification ──

    /// <summary>HR manually enters to cross-check. Must equal NettSalary (doc §4).</summary>
    public decimal? HrEnteredAmount { get; set; }

    /// <summary>Computed: HrEnteredAmount == NettSalary.</summary>
    public bool? AmountMatches { get; set; }

    // Navigation
    public PayrollRun PayrollRun { get; set; } = null!;
    public ICollection<PayrollDeduction> Deductions { get; set; } = [];
    public ICollection<PayrollAddition> Additions { get; set; } = [];
}
