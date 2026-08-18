using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Infrastructure.Notifications.Helpers;

namespace GerenciadorDeFinancas.Infrastructure.Notifications.Banks;

public sealed class InterNotificationParser : NotificationParserBase
{
    private static readonly string[] MerchantPhrasesToRemove =
    {
        "compra aprovada de", "compra no crédito de", "compra no débito de", "compra de",
    };

    public override string BankId => KnownBanks.Inter;

    protected override IReadOnlyList<string> SupportedPackages { get; } =
        new[] { KnownBanks.InterPackage };

    public override bool IsPurchaseLike(NotificationRaw notification) =>
        ContainsAny(Combined(notification), "compra", "aprovad", "débito", "crédito");

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
