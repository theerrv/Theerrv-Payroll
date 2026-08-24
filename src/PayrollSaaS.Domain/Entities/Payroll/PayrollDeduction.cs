using PayrollSaaS.Domain.Common;
using PayrollSaaS.Domain.Enums;

namespace PayrollSaaS.Domain.Entities.Payroll;

/// <summary>Itemized deduction per entry (doc §5.5). Types: lop | advance | other.</summary>
public class PayrollDeduction : BaseEntity
{
    public Guid PayrollEntryId { get; set; }
    public DeductionType DeductionType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    // Navigation
    public PayrollEntry PayrollEntry { get; set; } = null!;
}
