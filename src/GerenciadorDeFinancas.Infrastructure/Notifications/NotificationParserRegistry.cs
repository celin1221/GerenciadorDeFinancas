using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Application.Ports;

namespace GerenciadorDeFinancas.Infrastructure.Notifications;

public sealed class NotificationParserRegistry : INotificationParserRegistry
{
    private readonly IReadOnlyList<INotificationParser> _parsers;

    public NotificationParserRegistry(IEnumerable<INotificationParser> parsers)
    {
        _parsers = parsers
            .OrderByDescending(parser => parser.Priority)
            .ToList();
    }

    public IReadOnlyList<INotificationParser> Parsers => _parsers;

    public INotificationParser? Find(NotificationRaw notification) =>
        _parsers.FirstOrDefault(parser => parser.CanHandle(notification));
}
