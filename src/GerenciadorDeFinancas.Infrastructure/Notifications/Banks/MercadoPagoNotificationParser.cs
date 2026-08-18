using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Infrastructure.Notifications.Helpers;

namespace GerenciadorDeFinancas.Infrastructure.Notifications.Banks;

public sealed class MercadoPagoNotificationParser : NotificationParserBase
{
    private static readonly string[] MerchantPhrasesToRemove =
    {
        "você pagou", "pagamento de", "compra aprovada de", "compra de",
    };

    public override string BankId => KnownBanks.MercadoPago;

    protected override IReadOnlyList<string> SupportedPackages { get; } =
        new[] { KnownBanks.MercadoPagoPackage };

    public override bool IsPurchaseLike(NotificationRaw notification) =>
        ContainsAny(Combined(notification), "compra", "pagamento", "aprovad", "pagou");

    public override ParsedPurchase? TryParse(NotificationRaw notification)
    {
        if (!IsPurchaseLike(notification))
        {
            return null;
        }

        var amountCents = BrCurrencyParser.TryParseBrlParcelAware(Combined(notification));
        if (amountCents is null or <= 0)
        {
            return null;
        }

        var merchant = MerchantExtractor.Extract(MerchantSource(notification), MerchantPhrasesToRemove);
        if (string.IsNullOrWhiteSpace(merchant))
        {
            merchant = MerchantExtractor.Fallback(notification.Title, notification.Text);
        }

        return CreatePurchase(notification, merchant, amountCents.Value);
    }
}
