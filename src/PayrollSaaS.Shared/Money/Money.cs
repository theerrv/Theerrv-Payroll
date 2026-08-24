namespace PayrollSaaS.Shared.Money;

/// <summary>
/// Rounding policy for every monetary value in the system.
///
/// Storage is decimal(18,4) throughout (design doc section 5). Intermediate arithmetic keeps
/// 4 decimal places; values that are actually paid or reported to a third party are rounded to
/// 2 decimal places at that boundary only. Rounding is half-away-from-zero, which is what Indian
/// payroll conventionally uses -- NOT .NET's default banker's rounding (ToEven), which would
/// silently under/over-pay on exact-half paise.
/// </summary>
public static class MoneyMath
{
    public const int StorageScale = 4;
    public const int PayableScale = 2;

    /// <summary>Rounds to the 4dp storage scale. Use for every intermediate calculation.</summary>
    public static decimal Round(decimal value) =>
        decimal.Round(value, StorageScale, MidpointRounding.AwayFromZero);

    /// <summary>Rounds to 2dp. Use only at payable boundaries: nett salary, bank file, report totals.</summary>
    public static decimal RoundPayable(decimal value) =>
        decimal.Round(value, PayableScale, MidpointRounding.AwayFromZero);

    /// <summary>Serialisation format for money in API responses -- always 4dp, e.g. "42895.0000".</summary>
    public static string ToApiString(decimal value) =>
        Round(value).ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
}
