using PayrollSaaS.Domain.Common;
using PayrollSaaS.Domain.Enums;

namespace PayrollSaaS.Domain.Entities.Payroll;

/// <summary>
/// One per Division per Month (doc §5.5). UNIQUE (school_division_id, payroll_month).
/// Lifecycle: Draft → Submitted → Approved → Finalized (doc §2).
/// Illegal transitions throw InvalidOperationException → 409 via IExceptionHandler.
/// </summary>
public class PayrollRun : BaseEntity, IAuditable
{
    public Guid SchoolId { get; set; }
    public Guid SchoolDivisionId { get; set; }

    /// <summary>First day of the payroll month, e.g. 2025-08-01.</summary>
    public DateOnly PayrollMonth { get; set; }

    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;

    public Guid CreatedBy { get; set; }
    public Guid? SubmittedBy { get; set; }
    public Guid? ApprovedBy { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    /// <summary>Once set, all entries are locked and immutable (doc §5.5).</summary>
    public DateTime? FinalizedAt { get; set; }

    // Navigation
    public ICollection<PayrollEntry> Entries { get; set; } = [];

    // ────── State machine ──────

    public void Submit(Guid userId)
    {
        EnsureStatus(PayrollRunStatus.Draft, nameof(Submit));
        Status = PayrollRunStatus.Submitted;
        SubmittedBy = userId;
        SubmittedAt = DateTime.UtcNow;
    }

    public void Approve(Guid userId)
    {
        EnsureStatus(PayrollRunStatus.Submitted, nameof(Approve));
        Status = PayrollRunStatus.Approved;
        ApprovedBy = userId;
        ApprovedAt = DateTime.UtcNow;
    }

    public void FinalizeRun()
    {
        EnsureStatus(PayrollRunStatus.Approved, nameof(Finalize));
        Status = PayrollRunStatus.Finalized;
        FinalizedAt = DateTime.UtcNow;
    }

    public bool IsFinalized => Status == PayrollRunStatus.Finalized;

    private void EnsureStatus(PayrollRunStatus required, string action)
    {
        if (Status != required)
            throw new InvalidOperationException(
                $"Cannot {action}: payroll run is {Status}, must be {required}.");
    }
}
