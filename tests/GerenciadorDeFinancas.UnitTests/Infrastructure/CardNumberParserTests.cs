using GerenciadorDeFinancas.Infrastructure.Notifications.Helpers;

namespace GerenciadorDeFinancas.UnitTests.Infrastructure;

public class CardNumberParserTests
{
    [Theory]
    [InlineData("com cartão final 1234", "1234")]
    [InlineData("cartão terminando em 4321", "4321")]
    [InlineData("últimos dígitos 9999", "9999")]
    [InlineData("Cartão de crédito final 5678 - Compra aprovada", "5678")]
    public void ExtractLast4_ReturnsLast4(string text, string expected)
    {
        Assert.Equal(expected, CardNumberParser.ExtractLast4(text));
    }

    [Theory]
    [InlineData("Compra de R$ 35,90 no iFood")]
    [InlineData("")]
    [InlineData(null)]
    public void ExtractLast4_ReturnsNullWhenNoCardReference(string? text)
    {
        Assert.Null(CardNumberParser.ExtractLast4(text));
    }
}
