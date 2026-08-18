using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Infrastructure.Notifications.Helpers;

namespace GerenciadorDeFinancas.Infrastructure.Notifications.Banks;

public sealed class BancoDoBrasilNotificationParser : NotificationParserBase
{
    private static readonly string[] MerchantPhrasesToRemove =
    {
        "cartão de crédito", "cartão de débito", "compra aprovada de", "compra de",
    };

    public override string BankId => KnownBanks.BancoDoBrasil;

    protected override IReadOnlyList<string> SupportedPackages { get; } =
        new[] { KnownBanks.BancoDoBrasilPackage };

    public override bool IsPurchaseLike(NotificationRaw notification) =>
        ContainsAny(Combined(notification), "compra", "aprovad", "cartão de crédito");

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
