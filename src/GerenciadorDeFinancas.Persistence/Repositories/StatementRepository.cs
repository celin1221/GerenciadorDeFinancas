using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GerenciadorDeFinancas.Persistence.Repositories;

public sealed class StatementRepository : IStatementRepository
{
    private readonly FinanceDbContext _context;

    public StatementRepository(FinanceDbContext context)
    {
        _context = context;
    }

    public Task<Statement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Statements.FirstOrDefaultAsync(statement => statement.Id == id, cancellationToken);

    public Task<Statement?> GetOpenForCardAsync(Guid cardId, CancellationToken cancellationToken = default) =>
        _context.Statements.FirstOrDefaultAsync(
            statement => statement.CardId == cardId && statement.Status == Domain.Enums.StatementStatus.Open,
            cancellationToken);

    public async Task<IReadOnlyList<Statement>> ListByCardAsync(Guid cardId, CancellationToken cancellationToken = default) =>
        await _context.Statements
            .Where(statement => statement.CardId == cardId)
            .OrderByDescending(statement => statement.YearMonth)
            .ToListAsync(cancellationToken);

    public void Add(Statement statement) => _context.Statements.Add(statement);

    public void Update(Statement statement) => _context.Statements.Update(statement);
}
