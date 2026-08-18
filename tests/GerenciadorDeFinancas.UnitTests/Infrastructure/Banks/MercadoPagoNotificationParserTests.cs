using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Infrastructure.Notifications.Banks;

namespace GerenciadorDeFinancas.UnitTests.Infrastructure.Banks;

public class MercadoPagoNotificationParserTests
{
    private readonly MercadoPagoNotificationParser _parser = new();

    public static TheoryData<NotificationRaw, long, string, string?> Corpus => new()
    {
        {
            Raw("Você pagou R$ 89,90 a Mercado Livre", "O valor vai entrar na próxima fatura do seu Cartão Mercado Pago."),
            8990,
            "Mercado Livre",
            null
        },
        {
            Raw("Você pagou R$ 450,00 a Loja Exemplo", "O valor vai entrar na próxima fatura do seu Cartão Mercado Pago."),
            45000,
            "Loja Exemplo",
            null
        },
        {
            Raw("Você pagou R$ 12,50 a Farmácia Central", "O valor vai entrar na próxima fatura do seu Cartão Mercado Pago."),
            1250,
            "Farmácia Central",
            null
        },
    };

    [Theory]
    [MemberData(nameof(Corpus))]
    public void TryParse_ExtractsPurchase(NotificationRaw raw, long amountCents, string merchant, string? last4)
    {
        var parsed = _parser.TryParse(raw);

        Assert.NotNull(parsed);
        Assert.Equal(KnownBanks.MercadoPago, parsed.BankId);
        Assert.Equal(amountCents, parsed.AmountCents);
        Assert.Equal(merchant, parsed.MerchantName);
        Assert.Equal(last4, parsed.CardLast4);
    }

    [Theory]
    [InlineData("Seu saldo mudou", "Você recebeu uma transferência de R$ 50,00.")]
    [InlineData("teste", "teste")]
    public void TryParse_NonPurchase_ReturnsNull(string title, string text)
    {
        Assert.Null(_parser.TryParse(Raw(title, text)));
    }

    private static NotificationRaw Raw(string title, string text) =>
        new(KnownBanks.MercadoPagoPackage, title, text, "key-mp", DateTimeOffset.UtcNow);
}
