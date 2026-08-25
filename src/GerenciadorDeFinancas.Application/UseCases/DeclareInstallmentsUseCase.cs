using GerenciadorDeFinancas.Application.Services;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Entities;
using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Domain.Exceptions;
using GerenciadorDeFinancas.Domain.ValueObjects;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class DeclareInstallmentsUseCase
{
    public const int MaxInstallments = 48;

    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public DeclareInstallmentsUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<IReadOnlyList<Guid>> ExecuteAsync(
        Guid purchaseId,
        int totalInstallments,
        CancellationToken cancellationToken = default)
    {
        if (totalInstallments is < 2 or > MaxInstallments)
        {
            throw new DomainException($"Número de parcelas deve estar entre 2 e {MaxInstallments}.");
        }

        using var unitOfWork = _unitOfWorkFactory.Create();

        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId, cancellationToken)
            ?? throw new DomainException("Compra não encontrada.");
        if (purchase.Status != PurchaseStatus.Pending)
        {
            throw new DomainException("Apenas compras pendentes podem ser parceladas.");
        }

        var card = await unitOfWork.Cards.GetByIdAsync(purchase.CardId, cancellationToken)
            ?? throw new DomainException("Cartão não encontrado.");

        var parts = Money.SplitEvenlyIntoParts(Money.FromCents(purchase.AmountCents), totalInstallments);
        var groupId = Guid.NewGuid();
        var purchaseIds = new List<Guid> { purchase.Id };

        purchase.ConvertToFirstInstallment(groupId, totalInstallments, parts[0].Cents);
        if (purchase.StatementId is null)
        {
            var firstStatement = await StatementResolver.ResolveAsync(unitOfWork, card, purchase.Date, cancellationToken);
            purchase.MoveToStatement(firstStatement.Id);
        }

        for (var i = 1; i < totalInstallments; i++)
        {
            var date = purchase.Date.AddMonths(i);
            var statement = await StatementResolver.ResolveAsync(unitOfWork, card, date, cancellationToken);
            var sibling = new Purchase(
                cardId: purchase.CardId,
                amountCents: parts[i].Cents,
                date: date,
                description: purchase.Description,
                merchantId: purchase.MerchantId,
                statementId: statement.Id,
                installmentGroupId: groupId,
                installmentNumber: i + 1,
                totalInstallments: totalInstallments);
            unitOfWork.Purchases.Add(sibling);
            purchaseIds.Add(sibling.Id);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return purchaseIds;
    }
}
