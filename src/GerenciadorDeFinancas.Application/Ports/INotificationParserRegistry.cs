using GerenciadorDeFinancas.Application.Dtos;

namespace GerenciadorDeFinancas.Application.Ports;

public interface INotificationParserRegistry
{
    INotificationParser? Find(NotificationRaw notification);

    IReadOnlyList<INotificationParser> Parsers { get; }
}
