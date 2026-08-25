using GerenciadorDeFinancas.Domain.Entities;

namespace GerenciadorDeFinancas.Domain.Abstractions;

public interface IStatementRepository
{
    Task<Statement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Statement?> GetOpenForCardAsync(Guid cardId, CancellationToken cancellationToken = default);

    Task<Statement?> GetByCardAndYearMonthAsync(Guid cardId, int yearMonth, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Statement>> ListByCardAsync(Guid cardId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Statement>> ListAllAsync(CancellationToken cancellationToken = default);

    void Add(Statement statement);

    void Update(Statement statement);
}
