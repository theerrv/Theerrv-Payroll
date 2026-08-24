namespace PayrollSaaS.Domain.Enums;

/// <summary>
/// Earning or deduction. salary_after_pf = SUM(earning). Deduction components are emitted as
/// itemized payroll_deductions of type Other — they do NOT reduce salary_after_pf.
/// SPEC-GAP: see gap 1 in the design doc analysis.
/// </summary>
public enum ComponentType { Earning, Deduction }
