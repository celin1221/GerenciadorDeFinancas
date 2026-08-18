using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Infrastructure.Notifications.Helpers;

namespace GerenciadorDeFinancas.Infrastructure.Notifications;

public sealed partial class GenericNotificationParser : NotificationParserBase
{
    public override string BankId => KnownBanks.Generic;

    public override int Priority => 0;

    protected override IReadOnlyList<string> SupportedPackages { get; } = Array.Empty<string>();

    public override bool CanHandle(NotificationRaw notification) => true;

    public override ParsedPurchase? TryParse(NotificationRaw notification)
    {
        var combined = $"{notification.Title} {notification.Text}";
        var amountCents = BrCurrencyParser.TryParseBrl(combined);
        if (amountCents is null or <= 0)
        {
            return null;
        }

        var merchantName = ExtractMerchant(notification);
        var description = string.Join(" | ",
            new[] { notification.Title, notification.Text }.Where(part => !string.IsNullOrWhiteSpace(part)));

        return new ParsedPurchase(
            BankId: BankId,
            MerchantName: merchantName,
            AmountCents: amountCents.Value,
            Date: notification.PostedAt.LocalDateTime,
            Description: description,
            BankRefId: notification.NotificationKey,
            CardLast4: CardNumberParser.ExtractLast4(combined));
    }

    private static string ExtractMerchant(NotificationRaw notification)
    {
        var source = !string.IsNullOrWhiteSpace(notification.Title)
            ? notification.Title
            : notification.Text.Split('\n').FirstOrDefault() ?? string.Empty;
        return source.Length > 200 ? source[..200] : source;
    }
}
