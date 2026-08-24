using PayrollSaaS.Shared.Money;
using Shouldly;
using Xunit;

namespace PayrollSaaS.UnitTests.Shared;

public class MoneyMathTests
{
    [Theory]
    // Half-away-from-zero, NOT banker's rounding. Banker's would give 2.4567/2.4568 differently
    // on exact halves and silently shift paise on large payroll runs.
    [InlineData("2.00005", "2.0001")]
    [InlineData("2.00015", "2.0002")]
    [InlineData("-2.00005", "-2.0001")]
    public void Round_UsesHalfAwayFromZero_NotBankers(string input, string expected)
        => MoneyMath.Round(decimal.Parse(input)).ShouldBe(decimal.Parse(expected));

    [Theory]
    [InlineData("42895", "42895.0000")]
    [InlineData("0", "0.0000")]
    [InlineData("40225.8065", "40225.8065")]
    public void ToApiString_AlwaysEmitsFourDecimals(string input, string expected)
        => MoneyMath.ToApiString(decimal.Parse(input)).ShouldBe(expected);

    [Fact]
    public void RoundPayable_RoundsToTwoDecimals()
        => MoneyMath.RoundPayable(40225.8065m).ShouldBe(40225.81m);
}
