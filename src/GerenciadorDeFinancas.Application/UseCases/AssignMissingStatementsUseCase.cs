using GerenciadorDeFinancas.Application.Services;
using GerenciadorDeFinancas.Domain.Abstractions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class AssignMissingStatementsUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public AssignMissingStatementsUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var orphans = await unitOfWork.Purchases.ListWithoutStatementAsync(cancellationToken);
        var assigned = 0;
        foreach (var purchase in orphans)
        {
            if (purchase.Card is null)
            {
                continue;
            }

            var statement = await StatementResolver.ResolveAsync(unitOfWork, purchase.Card, purchase.Date, cancellationToken);
            purchase.MoveToStatement(statement.Id);
            assigned++;
        }

        if (assigned > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return assigned;
    }
}
