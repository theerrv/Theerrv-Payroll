using PayrollSaaS.Domain.Calculators;
using Shouldly;
using Xunit;

namespace PayrollSaaS.UnitTests.Calculators;

/// <summary>
/// Tests for PayrollCalculationService — the pure payroll formula engine.
/// Every test name describes the scenario, not the method.
/// </summary>
public class PayrollCalculationServiceTests
{
    private readonly PayrollCalculationService _sut = new();

    private static PayrollCalculationInput BaseInput(
        decimal salaryAfterPf = 43_000m,
        decimal pfGross = 14_000m,
        bool isPfEligible = true,
        int lopDays = 0,
        decimal esiRate = 0.0075m,
        decimal advanceInstallmentDue = 0m,
        IReadOnlyList<DeductionItem>? adHocDeductions = null,
        IReadOnlyList<AdditionItem>? adHocAdditions = null) => new()
    {
        SalaryAfterPf = salaryAfterPf,
        PfGross = pfGross,
        IsPfEligible = isPfEligible,
        LopDays = lopDays,
        EsiRate = esiRate,
        AdvanceInstallmentDue = advanceInstallmentDue,
        AdHocDeductions = adHocDeductions ?? [],
        AdHocAdditions = adHocAdditions ?? [],
    };

    /// <summary>
    /// The exact worked example from doc §4:
    /// Salary 43,000 / 0 LOP / PF Gross 14,000 → ESI 105 → NETT 42,895
    /// </summary>
    [Fact]
    public void DocWorkedExample_ZeroLop_CorrectNettSalary()
    {
        var result = _sut.Calculate(BaseInput());

        result.SalaryAfterPf.ShouldBe(43_000m);
        result.LopDays.ShouldBe(0);
        result.LopDeduction.ShouldBe(0m);
        result.GrossSalary.ShouldBe(43_000m);
        result.EsiAmount.ShouldBe(105m);
        result.NettSalary.ShouldBe(42_895m);
    }

    /// <summary>
    /// SPEC-GAP 9 regression: the doc's formula as written subtracts LOP twice.
    /// With 2 LOP days on ₹43,000 salary:
    ///   LOP = 43000/31 * 2 = 2774.1935 (4dp, half-away-from-zero)
    ///   Correct NETT Pay = 43000 - 2774.1935 = 40225.8065
    ///   Buggy NETT Pay   = (43000-2774.1935) - 2774.1935 = 37451.6130  ← WRONG
    /// </summary>
    [Fact]
    public void NettPay_DoesNotSubtractLopTwice()
    {
        var result = _sut.Calculate(BaseInput(lopDays: 2));

        result.LopDeduction.ShouldBe(2774.1935m);  // 43000/31*2 rounded to 4dp
        result.GrossSalary.ShouldBe(40225.8065m);   // shown on payslip
        result.NettPay.ShouldBe(40225.8065m);        // CORRECT: salaryAfterPf - totalDeductions
        result.NettPay.ShouldNotBe(37451.6130m);     // BUGGY: grossSalary - totalDeductions (double LOP)
        result.EsiAmount.ShouldBe(105m);
        result.NettSalary.ShouldBe(40120.8065m);     // 40225.8065 - 105
    }

    [Fact]
    public void PfIneligible_EsiIsZero()
    {
        var result = _sut.Calculate(BaseInput(isPfEligible: false));

        result.EsiAmount.ShouldBe(0m);
        result.NettSalary.ShouldBe(43_000m); // No deductions, no ESI
    }

    [Fact]
    public void AdvanceInstallment_DeductedFromNettPay()
    {
        var result = _sut.Calculate(BaseInput(advanceInstallmentDue: 5_000m));

        result.TotalDeductions.ShouldBe(5_000m);
        result.NettPay.ShouldBe(38_000m);          // 43000 - 5000
        result.NettSalary.ShouldBe(37_895m);        // 38000 - 105
        result.ItemizedDeductions.ShouldContain(d => d.Type == "Advance" && d.Amount == 5_000m);
    }

    [Fact]
    public void AdditionsAndDeductions_Combined()
    {
        var result = _sut.Calculate(BaseInput(
            advanceInstallmentDue: 2_000m,
            adHocDeductions: [new DeductionItem("Other", "Uniform", 500m)],
            adHocAdditions: [new AdditionItem("Bonus", "Festival Bonus", 3_000m)]));

        result.TotalDeductions.ShouldBe(2_500m);  // advance + uniform
        result.TotalAdditions.ShouldBe(3_000m);
        result.NettPay.ShouldBe(43_500m);          // 43000 - 2500 + 3000
        result.EsiAmount.ShouldBe(105m);
        result.NettSalary.ShouldBe(43_395m);        // 43500 - 105
    }

    [Fact]
    public void FullMonthLop_NettSalaryIsNonNegative()
    {
        var result = _sut.Calculate(BaseInput(lopDays: 31));

        result.LopDeduction.ShouldBe(43_000m);     // 43000/31*31
        result.GrossSalary.ShouldBe(0m);
        result.NettPay.ShouldBe(0m);
        result.NettSalary.ShouldBe(0m);             // ESI on 0 gross is not negative
        result.NettSalary.ShouldBeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void NonZeroLop_GrossSalaryShownOnPayslip()
    {
        var result = _sut.Calculate(BaseInput(lopDays: 5));

        // Gross = SalaryAfterPf - LOP, NOT the nett-pay figure
        result.LopDeduction.ShouldBe(6935.4839m);  // 43000/31*5 = 6935.48387.. → rounded 4dp
        result.GrossSalary.ShouldBe(36064.5161m);
    }

    /// <summary>
    /// All money values in the result must be at most 4dp — the storage/serialisation scale.
    /// </summary>
    [Fact]
    public void AllMoneyValues_AtMostFourDecimalPlaces()
    {
        var result = _sut.Calculate(BaseInput(lopDays: 3, advanceInstallmentDue: 1234.5678m));
        var values = new[] {
            result.LopDeduction, result.GrossSalary, result.TotalDeductions,
            result.TotalAdditions, result.NettPay, result.EsiAmount, result.NettSalary
        };
        foreach (var v in values)
        {
            var s = v.ToString("G29"); // no trailing zeros
            var dotIndex = s.IndexOf('.');
            if (dotIndex >= 0)
                (s.Length - dotIndex - 1).ShouldBeLessThanOrEqualTo(4,
                    $"Value {v} has more than 4 decimal places");
        }
    }
}
