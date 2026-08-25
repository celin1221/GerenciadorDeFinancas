using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Enums;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class ListInvoicesUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public ListInvoicesUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<IReadOnlyList<InvoiceItem>> ExecuteAsync(
        Guid? cardId = null,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var statements = cardId is { } id
            ? await unitOfWork.Statements.ListByCardAsync(id, cancellationToken)
            : await unitOfWork.Statements.ListAllAsync(cancellationToken);

        return statements
            .Select(statement => new InvoiceItem(
                statement.Id,
                statement.CardId,
                statement.Card?.Name ?? "Cartão",
                statement.YearMonth,
                statement.Purchases.Sum(purchase => purchase.AmountCents),
                statement.Purchases.Count,
                statement.Purchases.Count(purchase => purchase.Status == PurchaseStatus.Pending),
                FormatStatus(statement.Status)))
            .ToList();
    }

    private static string FormatStatus(StatementStatus status) => status switch
    {
        StatementStatus.Open => "Aberta",
        StatementStatus.Closed => "Fechada",
        StatementStatus.Paid => "Paga",
        _ => status.ToString(),
    };
}
