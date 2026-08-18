using GerenciadorDeFinancas.Infrastructure.Notifications.Helpers;

namespace GerenciadorDeFinancas.UnitTests.Infrastructure;

public class MerchantExtractorTests
{
    [Theory]
    [InlineData("Compra de R$ 35,90 na iFood", "iFood")]
    [InlineData("R$ 300,00 em 3x de R$ 100,00 no Mercado Livre", "Mercado Livre")]
    [InlineData("no Mercado Livre com cartão final 1234.", "Mercado Livre")]
    [InlineData("em SUPERMERCADO X", "SUPERMERCADO X")]
    public void Extract_RemovesPhrasesAndAmounts(string source, string expected)
    {
        var result = MerchantExtractor.Extract(source, "compra de");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Fallback_UsesTitleWhenMerchantIsEmpty()
    {
        Assert.Equal(
            "Compra no crédito de R$ 50,00",
            MerchantExtractor.Fallback("Compra no crédito de R$ 50,00", "Cartão final 1234."));
    }
}
