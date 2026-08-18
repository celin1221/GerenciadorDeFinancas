using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Infrastructure.Notifications.Banks;

namespace GerenciadorDeFinancas.UnitTests.Infrastructure.Banks;

public class NubankNotificationParserTests
{
    private readonly NubankNotificationParser _parser = new();

    public static TheoryData<NotificationRaw, long, string, string?> Corpus => new()
    {
        { Raw("Compra no crédito aprovada", "Compra de R$ 35,90 APROVADA em iFood para o cartão com final 1234."), 3590, "iFood", "1234" },
        { Raw("Compra no crédito aprovada", "Compra de R$ 300,00 APROVADA em Mercado Livre em 3x de R$ 100,00 para o cartão com final 5678."), 10000, "Mercado Livre", "5678" },
        { Raw("Compra no crédito aprovada", "Compra de R$ 89,00 APROVADA em Uber para o cartão com final 9012."), 8900, "Uber", "9012" },
    };

    [Theory]
    [MemberData(nameof(Corpus))]
    public void TryParse_ExtractsPurchase(NotificationRaw raw, long amountCents, string merchant, string? last4)
    {
        var parsed = _parser.TryParse(raw);

        Assert.NotNull(parsed);
        Assert.Equal(KnownBanks.Nubank, parsed.BankId);
        Assert.Equal(amountCents, parsed.AmountCents);
        Assert.Equal(merchant, parsed.MerchantName);
        Assert.Equal(last4, parsed.CardLast4);
    }

    [Theory]
    [InlineData("Seu limite foi alterado", "Solicite pelo app.")]
    [InlineData("teste", "teste")]
    [InlineData("Você recebeu um Pix de R$ 50,00", "Transfira pelo app.")]
    public void TryParse_NonPurchase_ReturnsNull(string title, string text)
    {
        Assert.Null(_parser.TryParse(Raw(title, text)));
    }

    private static NotificationRaw Raw(string title, string text) =>
        new(KnownBanks.NubankPackage, title, text, "key-nubank", DateTimeOffset.UtcNow);
}
