using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Exceptions;
using GerenciadorDeFinancas.Domain.ValueObjects;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class SplitPurchaseUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public SplitPurchaseUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task ExecuteEqualAsync(
        Guid purchaseId,
        IReadOnlyList<Guid> personIds,
        CancellationToken cancellationToken = default)
    {
        if (personIds.Count == 0)
        {
            throw new DomainException("Selecione ao menos uma pessoa para a divisão.");
        }

        if (personIds.Distinct().Count() != personIds.Count)
        {
            throw new DomainException("A mesma pessoa não pode participar mais de uma vez.");
        }

        using var unitOfWork = _unitOfWorkFactory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId, cancellationToken)
            ?? throw new DomainException("Compra não encontrada.");

        await ValidatePeopleAsync(unitOfWork, personIds, cancellationToken);

        var parts = Money.SplitEvenlyIntoParts(Money.FromCents(purchase.AmountCents), personIds.Count);
        var shares = personIds.Select((personId, index) => (personId, parts[index].Cents)).ToList();

        ApplyShares(unitOfWork, purchase, shares);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteCustomAsync(
        Guid purchaseId,
        IReadOnlyList<(Guid PersonId, long AmountCents)> shares,
        CancellationToken cancellationToken = default)
    {
        if (shares.Count == 0)
        {
            throw new DomainException("Selecione ao menos uma pessoa para a divisão.");
        }

        using var unitOfWork = _unitOfWorkFactory.Create();
        var purchase = await unitOfWork.Purchases.GetByIdAsync(purchaseId, cancellationToken)
            ?? throw new DomainException("Compra não encontrada.");

        await ValidatePeopleAsync(unitOfWork, shares.Select(share => share.PersonId), cancellationToken);

        ApplyShares(unitOfWork, purchase, shares);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyShares(
        IUnitOfWork unitOfWork,
        Domain.Entities.Purchase purchase,
        IReadOnlyList<(Guid PersonId, long AmountCents)> shares)
    {
        purchase.SetShares(shares);
        foreach (var share in purchase.Shares)
        {
            unitOfWork.Purchases.AddShare(share);
        }

        purchase.MarkClassified();
    }

    private static async Task ValidatePeopleAsync(
        IUnitOfWork unitOfWork,
        IEnumerable<Guid> personIds,
        CancellationToken cancellationToken)
    {
        foreach (var personId in personIds.Distinct())
        {
            if (await unitOfWork.Persons.GetByIdAsync(personId, cancellationToken) is null)
            {
                throw new DomainException("Pessoa não encontrada.");
            }
        }
    }
}
