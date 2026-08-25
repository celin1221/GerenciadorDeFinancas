using GerenciadorDeFinancas.Domain.Exceptions;
using GerenciadorDeFinancas.Domain.ValueObjects;

namespace GerenciadorDeFinancas.UnitTests.Domain;

public class StatementPeriodCalculatorTests
{
    [Theory]
    [InlineData(15, 2026, 3, 10, 202603, 2026, 2, 16, 2026, 3, 15)]
    [InlineData(15, 2026, 3, 15, 202603, 2026, 2, 16, 2026, 3, 15)]
    public void Calculate_PurchaseOnOrBeforeClosing_BelongsToCurrentMonth(
        int closingDay,
        int year, int month, int day,
        int expectedYearMonth,
        int openingYear, int openingMonth, int openingDay,
        int closingYear, int closingMonth, int closingDayExpected)
    {
        var period = StatementPeriodCalculator.Calculate(closingDay, new DateOnly(year, month, day));

        Assert.Equal(expectedYearMonth, period.YearMonth);
        Assert.Equal(new DateOnly(openingYear, openingMonth, openingDay), period.OpeningDate);
        Assert.Equal(new DateOnly(closingYear, closingMonth, closingDayExpected), period.ClosingDate);
    }

    [Fact]
    public void Calculate_PurchaseAfterClosing_GoesToNextMonth()
    {
        var period = StatementPeriodCalculator.Calculate(15, new DateOnly(2026, 3, 16));

        Assert.Equal(202604, period.YearMonth);
        Assert.Equal(new DateOnly(2026, 3, 16), period.OpeningDate);
        Assert.Equal(new DateOnly(2026, 4, 15), period.ClosingDate);
    }

    [Fact]
    public void Calculate_DecemberAfterClosing_RollsOverToNextYear()
    {
        var period = StatementPeriodCalculator.Calculate(20, new DateOnly(2026, 12, 25));

        Assert.Equal(202701, period.YearMonth);
        Assert.Equal(new DateOnly(2026, 12, 21), period.OpeningDate);
        Assert.Equal(new DateOnly(2027, 1, 20), period.ClosingDate);
    }

    [Fact]
    public void Calculate_ClosingDay31_ClampsToShortMonths()
    {
        var period = StatementPeriodCalculator.Calculate(31, new DateOnly(2026, 2, 15));

        Assert.Equal(202602, period.YearMonth);
        Assert.Equal(new DateOnly(2026, 1, 31).AddDays(1), period.OpeningDate);
        Assert.Equal(new DateOnly(2026, 2, 28), period.ClosingDate);
    }

    [Fact]
    public void Calculate_ClosingDay1_PurchaseOnFirstDayBelongsToSameMonth()
    {
        var period = StatementPeriodCalculator.Calculate(1, new DateOnly(2026, 3, 1));

        Assert.Equal(202603, period.YearMonth);
        Assert.Equal(new DateOnly(2026, 2, 2), period.OpeningDate);
        Assert.Equal(new DateOnly(2026, 3, 1), period.ClosingDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Calculate_RejectsInvalidClosingDay(int closingDay)
    {
        Assert.Throws<DomainException>(
            () => StatementPeriodCalculator.Calculate(closingDay, new DateOnly(2026, 3, 10)));
    }
}
