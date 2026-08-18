using GerenciadorDeFinancas.Domain.Entities;

namespace GerenciadorDeFinancas.Domain.Abstractions;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> ListAsync(CancellationToken cancellationToken = default);

    void Add(Person person);

    void Update(Person person);
}
