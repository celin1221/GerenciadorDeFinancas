using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.Ports;
using GerenciadorDeFinancas.Infrastructure.Notifications.Helpers;

namespace GerenciadorDeFinancas.Infrastructure.Notifications;

public abstract class NotificationParserBase : INotificationParser
{
    public abstract string BankId { get; }

    public virtual int Priority => 100;

    protected abstract IReadOnlyList<string> SupportedPackages { get; }

    public abstract ParsedPurchase? TryParse(NotificationRaw notification);

    public virtual bool CanHandle(NotificationRaw notification) =>
        SupportedPackages.Contains(notification.PackageName, StringComparer.OrdinalIgnoreCase);

    public virtual bool IsPurchaseLike(NotificationRaw notification) => true;

    protected static bool ContainsAny(string text, params string[] keywords) =>
        keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    protected static string Combined(NotificationRaw notification) =>
        $"{notification.Title} {notification.Text}";

    protected static string MerchantSource(NotificationRaw notification) =>
        !string.IsNullOrWhiteSpace(notification.Title)
            ? notification.Title
            : notification.Text.Split('\n').FirstOrDefault() ?? string.Empty;

    protected static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength];

    protected ParsedPurchase CreatePurchase(NotificationRaw notification, string merchant, long amountCents) =>
        new(
            BankId: BankId,
            MerchantName: merchant,
            AmountCents: amountCents,
            Date: notification.PostedAt.LocalDateTime,
            Description: Truncate(Combined(notification), 500),
            BankRefId: notification.NotificationKey,
            CardLast4: CardNumberParser.ExtractLast4(Combined(notification)));
}
