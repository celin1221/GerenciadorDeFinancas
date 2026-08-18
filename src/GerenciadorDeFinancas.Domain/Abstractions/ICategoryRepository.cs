using GerenciadorDeFinancas.Domain.Entities;

namespace GerenciadorDeFinancas.Domain.Abstractions;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);

    void Add(Category category);
}
