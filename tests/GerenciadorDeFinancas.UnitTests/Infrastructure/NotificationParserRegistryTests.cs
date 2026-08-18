using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.Ports;
using GerenciadorDeFinancas.Infrastructure.Notifications;
using GerenciadorDeFinancas.Infrastructure.Notifications.Banks;

namespace GerenciadorDeFinancas.UnitTests.Infrastructure;

public class NotificationParserRegistryTests
{
    private readonly NotificationParserRegistry _registry = new(new INotificationParser[]
    {
        new GenericNotificationParser(),
        new NubankNotificationParser(),
        new MercadoPagoNotificationParser(),
        new InterNotificationParser(),
        new BancoDoBrasilNotificationParser(),
    });

    [Theory]
    [InlineData(KnownBanks.NubankPackage)]
    [InlineData(KnownBanks.MercadoPagoPackage)]
    [InlineData(KnownBanks.InterPackage)]
    [InlineData(KnownBanks.BancoDoBrasilPackage)]
    public void Find_BankPackage_SelectsBankParserBeforeGeneric(string package)
    {
        var raw = new NotificationRaw(package, "Compra", "Compra de R$ 10,00", "key", DateTimeOffset.UtcNow);

        var parser = _registry.Find(raw);

        Assert.NotNull(parser);
        Assert.NotEqual(KnownBanks.Generic, parser.BankId);
    }

    [Fact]
    public void Find_UnknownPackage_SelectsGenericParser()
    {
        var raw = new NotificationRaw("com.app.qualquer", "Compra", "Compra de R$ 10,00", "key", DateTimeOffset.UtcNow);

        var parser = _registry.Find(raw);

        Assert.NotNull(parser);
        Assert.Equal(KnownBanks.Generic, parser.BankId);
    }
}
