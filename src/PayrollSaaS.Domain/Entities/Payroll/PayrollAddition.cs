using PayrollSaaS.Domain.Common;
using PayrollSaaS.Domain.Enums;

namespace PayrollSaaS.Domain.Entities.Payroll;

/// <summary>Itemized addition per entry (doc §5.5). Types: bonus | arrear | other.</summary>
public class PayrollAddition : BaseEntity
{
    public Guid PayrollEntryId { get; set; }
    public AdditionType AdditionType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    // Navigation
    public PayrollEntry PayrollEntry { get; set; } = null!;
}
