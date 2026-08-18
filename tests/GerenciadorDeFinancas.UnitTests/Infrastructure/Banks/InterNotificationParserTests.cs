using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Infrastructure.Notifications.Banks;

namespace GerenciadorDeFinancas.UnitTests.Infrastructure.Banks;

public class InterNotificationParserTests
{
    private readonly InterNotificationParser _parser = new();

    public static TheoryData<NotificationRaw, long, string, string?> Corpus => new()
    {
        {
            Raw("Compra aprovada de R$ 120,00 em SUPERMERCADO X", "Cartão final 1234."),
            12000,
            "SUPERMERCADO X",
            "1234"
        },
        {
            Raw("Compra no crédito de R$ 50,00", "Cartão final 1234."),
            5000,
            "Compra no crédito de R$ 50,00",
            "1234"
        },
    };

    [Theory]
    [MemberData(nameof(Corpus))]
    public void TryParse_ExtractsPurchase(NotificationRaw raw, long amountCents, string merchant, string? last4)
    {
        var parsed = _parser.TryParse(raw);

        Assert.NotNull(parsed);
        Assert.Equal(KnownBanks.Inter, parsed.BankId);
        Assert.Equal(amountCents, parsed.AmountCents);
        Assert.Equal(merchant, parsed.MerchantName);
        Assert.Equal(last4, parsed.CardLast4);
    }

    [Theory]
    [InlineData("Seu boleto foi gerado", "Pague até 15/08.")]
    [InlineData("teste", "teste")]
    public void TryParse_NonPurchase_ReturnsNull(string title, string text)
    {
        Assert.Null(_parser.TryParse(Raw(title, text)));
    }

    private static NotificationRaw Raw(string title, string text) =>
        new(KnownBanks.InterPackage, title, text, "key-inter", DateTimeOffset.UtcNow);
}
