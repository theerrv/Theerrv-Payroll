using PayrollSaaS.Shared.Money;

namespace PayrollSaaS.Domain.Calculators;

/// <summary>
/// The heart of the system (doc §4). Pure: no DB, no clock, no I/O — therefore
/// exhaustively unit-testable (doc §7, Single Responsibility).
///
/// SPEC-GAP 9: The doc's formula as written subtracts LOP twice. This implementation
/// uses the corrected formula: NETT Pay = Salary After PF − Total Deductions + Total Additions.
/// See the regression test NettPay_DoesNotSubtractLopTwice.
/// </summary>
public sealed class PayrollCalculationService
{
    /// <summary>
    /// LOP divisor is always 31 regardless of actual month length (doc §4, explicitly stated).
    /// </summary>
    private const int LopDivisor = 31;

    public PayrollCalculationResult Calculate(PayrollCalculationInput input)
    {
        // Step 1: LOP
        var lopDeduction = MoneyMath.Round(input.SalaryAfterPf / LopDivisor * input.LopDays);

        // Step 2: Gross (shown on the payslip)
        var grossSalary = MoneyMath.Round(input.SalaryAfterPf - lopDeduction);

        // Step 3: Collect all deductions
        var deductions = new List<DeductionItem>();

        // LOP is always first
        if (lopDeduction > 0)
            deductions.Add(new DeductionItem("Lop", "Loss of Pay", lopDeduction));

        // Advance installment
        if (input.AdvanceInstallmentDue > 0)
            deductions.Add(new DeductionItem("Advance", "Advance Recovery", input.AdvanceInstallmentDue));

        // Ad-hoc deductions
        deductions.AddRange(input.AdHocDeductions);

        var totalDeductions = MoneyMath.Round(deductions.Sum(d => d.Amount));

        // Step 4: Collect all additions
        var additions = new List<AdditionItem>(input.AdHocAdditions);
        var totalAdditions = MoneyMath.Round(additions.Sum(a => a.Amount));

        // Step 5: NETT Pay — SPEC-GAP 9: NOT grossSalary - totalDeductions, which double-counts LOP.
        // Correct: salaryAfterPf - totalDeductions + totalAdditions
        var nettPay = MoneyMath.Round(input.SalaryAfterPf - totalDeductions + totalAdditions);

        // Step 6: ESI — SPEC-GAP 2: only applies to PF-eligible employees (doc §4)
        // SPEC-GAP: If nett pay is zero or negative (full-month LOP), ESI should not make it worse.
        // The doc does not address this edge case.
        var esiAmount = input.IsPfEligible
            ? MoneyMath.Round(input.PfGross * input.EsiRate)
            : 0m;

        // Step 7: Final pay
        var nettSalary = Math.Max(0m, MoneyMath.Round(nettPay - esiAmount));

        return new PayrollCalculationResult
        {
            SalaryAfterPf = input.SalaryAfterPf,
            LopDays = input.LopDays,
            LopDeduction = lopDeduction,
            GrossSalary = grossSalary,
            TotalDeductions = totalDeductions,
            TotalAdditions = totalAdditions,
            NettPay = nettPay,
            EsiAmount = esiAmount,
            NettSalary = nettSalary,
            ItemizedDeductions = deductions,
            ItemizedAdditions = additions
        };
    }
}
