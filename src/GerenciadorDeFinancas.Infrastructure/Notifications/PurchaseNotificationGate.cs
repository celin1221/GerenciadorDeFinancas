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
            System.Diagnostics.Debug.WriteLine($"GDF_Gate: package '{notification.PackageName}' não é banco conhecido");
            return false;
        }

        var parser = registry.Find(notification);
        if (parser is null)
        {
            System.Diagnostics.Debug.WriteLine($"GDF_Gate: nenhum parser encontrado para '{notification.PackageName}'");
            return false;
        }

        if (!parser.IsPurchaseLike(notification))
        {
            System.Diagnostics.Debug.WriteLine($"GDF_Gate: parser '{parser.BankId}' rejeitou (IsPurchaseLike=false)");
            return false;
        }

        return true;
    }
}
