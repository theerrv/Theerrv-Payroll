namespace PayrollSaaS.Domain.Calculators;

/// <summary>
/// Immutable snapshot of everything the calculation engine needs for one employee.
/// Assembled by CreatePayrollRunHandler from attendance, salary components, PF config, and advances.
/// </summary>
public sealed record PayrollCalculationInput
{
    /// <summary>SUM of active earning components. Fixed per employee — not recalculated monthly.</summary>
    public required decimal SalaryAfterPf { get; init; }

    /// <summary>Explicitly configured PF Gross (e.g. ₹14,000). Falls back to Basic if absent.</summary>
    public required decimal PfGross { get; init; }

    /// <summary>True when pf_status == Active AND pf_active_from &lt;= last day of payroll month.</summary>
    public required bool IsPfEligible { get; init; }

    /// <summary>COUNT of absent days in the payroll month.</summary>
    public required int LopDays { get; init; }

    /// <summary>ESI rate from SchoolPayrollSettings (default 0.0075).</summary>
    public required decimal EsiRate { get; init; }

    /// <summary>Advance installment amount due this month. 0 if none.</summary>
    public required decimal AdvanceInstallmentDue { get; init; }

    /// <summary>Ad-hoc deductions entered by HR for this entry.</summary>
    public IReadOnlyList<DeductionItem> AdHocDeductions { get; init; } = [];

    /// <summary>Ad-hoc additions entered by HR for this entry.</summary>
    public IReadOnlyList<AdditionItem> AdHocAdditions { get; init; } = [];
}
