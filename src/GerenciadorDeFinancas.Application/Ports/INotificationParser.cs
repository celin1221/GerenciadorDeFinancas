using GerenciadorDeFinancas.Application.Dtos;

namespace GerenciadorDeFinancas.Application.Ports;

public interface INotificationParser
{
    string BankId { get; }

    int Priority { get; }

    bool CanHandle(NotificationRaw notification);

    bool IsPurchaseLike(NotificationRaw notification) => true;

    ParsedPurchase? TryParse(NotificationRaw notification);
}
