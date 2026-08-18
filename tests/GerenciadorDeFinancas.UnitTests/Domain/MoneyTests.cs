using GerenciadorDeFinancas.Domain.ValueObjects;

namespace GerenciadorDeFinancas.UnitTests.Domain;

public class MoneyTests
{
    [Theory]
    [InlineData("1,23", 123)]
    [InlineData("0,10", 10)]
    [InlineData("180,00", 18000)]
    [InlineData("0,01", 1)]
    public void FromDecimal_RoundsToCents(string raw, long expectedCents)
    {
        var value = decimal.Parse(raw, System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        var money = Money.FromDecimal(value);

        Assert.Equal(expectedCents, money.Cents);
    }

    [Theory]
    [InlineData(100, 3)]
    [InlineData(105, 2)]
    [InlineData(10, 3)]
    [InlineData(1, 3)]
    [InlineData(1000, 7)]
    [InlineData(999, 3)]
    public void SplitEvenlyIntoParts_SumMatchesTotal(long totalCents, int parts)
    {
        var partsList = Money.SplitEvenlyIntoParts(Money.FromCents(totalCents), parts);

        Assert.Equal(parts, partsList.Count);
        Assert.Equal(totalCents, partsList.Sum(part => part.Cents));
        Assert.True(partsList.Max(part => part.Cents) - partsList.Min(part => part.Cents) <= 1);
    }

    [Fact]
    public void SplitEvenlyIntoParts_OneHundredByThree()
    {
        var parts = Money.SplitEvenlyIntoParts(Money.FromCents(100), 3);

        Assert.Equal(new[] { 34L, 33L, 33L }, parts.Select(part => part.Cents));
    }

    [Fact]
    public void SplitEvenlyIntoParts_InvalidPartsThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.SplitEvenlyIntoParts(Money.Zero, 0));
    }

    [Fact]
    public void Amount_ConvertsCentsToDecimal()
    {
        var money = Money.FromCents(123456);

        Assert.Equal(1234.56m, money.Amount);
    }
}
