namespace PayrollSaaS.Domain.Calculators;

/// <summary>Mirror of IDeductionCalculator for additions (bonus, arrear, ad-hoc).</summary>
public interface IAdditionCalculator
{
    IReadOnlyList<AdditionItem> Calculate(PayrollCalculationInput input);
}

public sealed record AdditionItem(string Type, string Description, decimal Amount);
