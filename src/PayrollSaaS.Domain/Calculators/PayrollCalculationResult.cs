namespace PayrollSaaS.Domain.Calculators;

/// <summary>Output of PayrollCalculationService.Calculate — all values to 4dp.</summary>
public sealed record PayrollCalculationResult
{
    public required decimal SalaryAfterPf { get; init; }
    public required int LopDays { get; init; }
    public required decimal LopDeduction { get; init; }
    public required decimal GrossSalary { get; init; }
    public required decimal TotalDeductions { get; init; }
    public required decimal TotalAdditions { get; init; }
    public required decimal NettPay { get; init; }
    public required decimal EsiAmount { get; init; }
    public required decimal NettSalary { get; init; }

    public required IReadOnlyList<DeductionItem> ItemizedDeductions { get; init; }
    public required IReadOnlyList<AdditionItem> ItemizedAdditions { get; init; }
}
