using PayrollSaaS.Domain.Common;
using PayrollSaaS.Domain.Enums;

namespace PayrollSaaS.Domain.Entities.Advances;

/// <summary>
/// One row per month per advance (doc §5.4). Status: pending | deducted | skipped.
/// Linked to payroll_entry_id once processed at finalize.
/// </summary>
public class AdvanceInstallment : BaseEntity
{
    public Guid AdvanceId { get; set; }

    /// <summary>First day of the month this installment covers.</summary>
    public DateOnly DueMonth { get; set; }

    public decimal Amount { get; set; }

    public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;

    /// <summary>Set when the installment is deducted during payroll finalize.</summary>
    public Guid? PayrollEntryId { get; set; }

    // Navigation
    public Advance Advance { get; set; } = null!;
}
