using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.Ports;
using GerenciadorDeFinancas.Infrastructure.Notifications;
using GerenciadorDeFinancas.Infrastructure.Notifications.Banks;

namespace GerenciadorDeFinancas.UnitTests.Infrastructure;

public class PurchaseNotificationGateTests
{
    private static readonly INotificationParserRegistry Registry = new NotificationParserRegistry(
        new INotificationParser[]
        {
            new NubankNotificationParser(),
            new MercadoPagoNotificationParser(),
            new InterNotificationParser(),
            new BancoDoBrasilNotificationParser(),
        });

    [Theory]
    [InlineData("com.app.qualquer", "Compra aprovada", "Compra de R$ 100,00 no mercado")]
    [InlineData("com.android.settings", "teste", "teste")]
    public void ShouldProcess_NonBankPackage_ReturnsFalse(string package, string title, string text)
    {
        var raw = new NotificationRaw(package, title, text, "key", DateTimeOffset.UtcNow);

        Assert.False(PurchaseNotificationGate.ShouldProcess(raw, Registry));
    }

    [Theory]
    [InlineData(KnownBanks.NubankPackage, "Compra de R$ 35,90 na iFood")]
    [InlineData(KnownBanks.MercadoPagoPackage, "Você pagou R$ 89,90 a Mercado Livre")]
    [InlineData(KnownBanks.InterPackage, "Compra no crédito de R$ 50,00")]
    [InlineData(KnownBanks.BancoDoBrasilPackage, "Compra aprovada de R$ 120,00")]
    public void ShouldProcess_BankPackageWithPurchaseText_ReturnsTrue(string package, string text)
    {
        var raw = new NotificationRaw(package, "Compra", text, "key", DateTimeOffset.UtcNow);

        Assert.True(PurchaseNotificationGate.ShouldProcess(raw, Registry));
    }

    [Theory]
    [InlineData(KnownBanks.NubankPackage, "teste", "teste")]
    [InlineData(KnownBanks.MercadoPagoPackage, "Seu saldo mudou", "Você recebeu uma transferência de R$ 50,00.")]
    [InlineData(KnownBanks.InterPackage, "Aniversário da conta", "Complete mais um ano conosco!")]
    [InlineData(KnownBanks.BancoDoBrasilPackage, "Aviso", "Seu token foi solicitado.")]
    public void ShouldProcess_BankPackageWithNonPurchaseText_ReturnsFalse(string package, string title, string text)
    {
        var raw = new NotificationRaw(package, title, text, "key", DateTimeOffset.UtcNow);

        Assert.False(PurchaseNotificationGate.ShouldProcess(raw, Registry));
    }
}
