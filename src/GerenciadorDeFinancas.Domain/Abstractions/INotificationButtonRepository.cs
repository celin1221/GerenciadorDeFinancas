using GerenciadorDeFinancas.Domain.Entities;

namespace GerenciadorDeFinancas.Domain.Abstractions;

public interface INotificationButtonRepository
{
    Task<IReadOnlyList<NotificationButton>> ListOrderedAsync(CancellationToken cancellationToken = default);

    Task<NotificationButton?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<NotificationButton?> GetWithPersonsAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(NotificationButton button);

    void Remove(NotificationButton button);
}
