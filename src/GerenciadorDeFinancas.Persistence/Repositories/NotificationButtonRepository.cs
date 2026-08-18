using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancas.Persistence.Repositories;

public sealed class NotificationButtonRepository : INotificationButtonRepository
{
    private readonly FinanceDbContext _context;

    public NotificationButtonRepository(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<NotificationButton>> ListOrderedAsync(CancellationToken cancellationToken = default) =>
        await _context.NotificationButtons
            .Include(button => button.Persons)
                .ThenInclude(bp => bp.Person)
            .OrderBy(button => button.Order)
            .ToListAsync(cancellationToken);

    public Task<NotificationButton?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.NotificationButtons.FirstOrDefaultAsync(button => button.Id == id, cancellationToken);

    public Task<NotificationButton?> GetWithPersonsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.NotificationButtons
            .Include(button => button.Persons)
                .ThenInclude(bp => bp.Person)
            .FirstOrDefaultAsync(button => button.Id == id, cancellationToken);

    public void Add(NotificationButton button) => _context.NotificationButtons.Add(button);

    public void Remove(NotificationButton button) => _context.NotificationButtons.Remove(button);
}
