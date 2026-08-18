using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Infrastructure.Notifications.Banks;

namespace GerenciadorDeFinancas.UnitTests.Infrastructure.Banks;

public class BancoDoBrasilNotificationParserTests
{
    private readonly BancoDoBrasilNotificationParser _parser = new();

    public static TheoryData<NotificationRaw, long, string, string?> Corpus => new()
    {
        {
            Raw("Cartão de crédito final 1234 - Compra aprovada de R$ 120,00 em SUPERMERCADO X", "Compra no cartão de crédito."),
            12000,
            "SUPERMERCADO X",
            "1234"
        },
        {
            Raw("Cartão de débito final 4321 - Compra de R$ 30,00 na Farmácia Y", "Compra no débito."),
            3000,
            "Farmácia Y",
            "4321"
        },
    };

    [Theory]
    [MemberData(nameof(Corpus))]
    public void TryParse_ExtractsPurchase(NotificationRaw raw, long amountCents, string merchant, string? last4)
    {
        var parsed = _parser.TryParse(raw);

        Assert.NotNull(parsed);
        Assert.Equal(KnownBanks.BancoDoBrasil, parsed.BankId);
        Assert.Equal(amountCents, parsed.AmountCents);
        Assert.Equal(merchant, parsed.MerchantName);
        Assert.Equal(last4, parsed.CardLast4);
    }

    [Theory]
    [InlineData("Aviso de segurança", "Solicite seu token pelo app.")]
    [InlineData("teste", "teste")]
    public void TryParse_NonPurchase_ReturnsNull(string title, string text)
    {
        Assert.Null(_parser.TryParse(Raw(title, text)));
    }

    private static NotificationRaw Raw(string title, string text) =>
        new(KnownBanks.BancoDoBrasilPackage, title, text, "key-bb", DateTimeOffset.UtcNow);
}
