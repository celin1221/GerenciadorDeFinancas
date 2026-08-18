using GerenciadorDeFinancas.Infrastructure.Notifications.Helpers;

namespace GerenciadorDeFinancas.UnitTests.Infrastructure;

public class BrCurrencyParserTests
{
    [Theory]
    [InlineData("R$ 180,00", 18000)]
    [InlineData("Compra aprovada de R$ 35,90 no iFood", 3590)]
    [InlineData("R$ 1.234,56", 123456)]
    [InlineData("R$ 1234,56", 123456)]
    [InlineData("R$ 50", 5000)]
    [InlineData("valor R$0,99", 99)]
    public void TryParseBrl_ExtractsAmount(string text, long expectedCents)
    {
        var result = BrCurrencyParser.TryParseBrl(text);

        Assert.Equal(expectedCents, result);
    }

    [Theory]
    [InlineData("Nenhum valor aqui")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParseBrl_ReturnsNullWhenNoAmount(string? text)
    {
        Assert.Null(BrCurrencyParser.TryParseBrl(text));
    }

    [Theory]
    [InlineData("Compra de R$ 300,00 em 3x de R$ 100,00", 10000)]
    [InlineData("R$ 300,00 em 10x de R$ 30,00", 3000)]
    [InlineData("Compra em 12x de R$ 45,99", 4599)]
    [InlineData("3x de R$ 100,00", 10000)]
    [InlineData("Compra de R$ 35,90 no iFood", 3590)]
    [InlineData("Compra de R$ 1.234,56 no mercado", 123456)]
    public void TryParseBrlParcelAware_PrefersParcelValue(string text, long expectedCents)
    {
        Assert.Equal(expectedCents, BrCurrencyParser.TryParseBrlParcelAware(text));
    }

    [Theory]
    [InlineData("Nenhum valor aqui")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParseBrlParcelAware_ReturnsNullWhenNoAmount(string? text)
    {
        Assert.Null(BrCurrencyParser.TryParseBrlParcelAware(text));
    }
}
