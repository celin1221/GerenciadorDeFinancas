using GerenciadorDeFinancas.Application.Banks;
using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.Ports;

namespace GerenciadorDeFinancas.Infrastructure.Notifications;

public static class PurchaseNotificationGate
{
    public static bool ShouldProcess(NotificationRaw notification, INotificationParserRegistry registry)
    {
        if (!KnownBanks.KnownBankPackages.Contains(notification.PackageName, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var parser = registry.Find(notification);
        return parser is not null && parser.IsPurchaseLike(notification);
    }
}
