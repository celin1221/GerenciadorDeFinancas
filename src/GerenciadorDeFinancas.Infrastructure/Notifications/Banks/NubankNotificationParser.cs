using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Infrastructure.Notifications.Helpers;

namespace GerenciadorDeFinancas.Infrastructure.Notifications.Banks;

public sealed class NubankNotificationParser : NotificationParserBase
{
    private static readonly string[] MerchantPhrasesToRemove =
    {
        "compra de", "compra aprovada de", "aprovada em", "pagamento aprovado de", "pagamento de",
    };

    public override string BankId => KnownBanks.Nubank;

    protected override IReadOnlyList<string> SupportedPackages { get; } =
        new[] { KnownBanks.NubankPackage };

    public override bool IsPurchaseLike(NotificationRaw notification) =>
        ContainsAny(Combined(notification), "compra", "pagamento", "aprovad");

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

        var merchantSource = !string.IsNullOrWhiteSpace(notification.Text)
            ? notification.Text.Split('\n').FirstOrDefault() ?? string.Empty
            : notification.Title;
        var merchant = MerchantExtractor.Extract(merchantSource, MerchantPhrasesToRemove);
        if (string.IsNullOrWhiteSpace(merchant))
        {
            merchant = MerchantExtractor.Fallback(notification.Title, notification.Text);
        }

        return CreatePurchase(notification, merchant, amountCents.Value);
    }
}
