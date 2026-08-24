namespace PayrollSaaS.Domain.Calculators;

/// <summary>
/// Open/Closed extension point (doc §7). The payroll engine iterates
/// IEnumerable&lt;IDeductionCalculator&gt;, calling Calculate on each.
/// Adding TDS later = one new class + one DI registration, zero changes to PayrollCalculationService.
/// </summary>
public interface IDeductionCalculator
{
    /// <summary>
    /// Returns zero or more itemized deductions for a single employee entry.
    /// Pure — no side effects, no I/O.
    /// </summary>
    IReadOnlyList<DeductionItem> Calculate(PayrollCalculationInput input);
}

/// <summary>A single line item (e.g. "LOP Deduction", ₹2,774.19).</summary>
public sealed record DeductionItem(string Type, string Description, decimal Amount);
