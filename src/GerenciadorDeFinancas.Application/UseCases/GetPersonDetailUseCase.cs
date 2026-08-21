using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Domain.Abstractions;

namespace GerenciadorDeFinancas.Application.UseCases;

public sealed class GetPersonDetailUseCase
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public GetPersonDetailUseCase(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<PersonDetail?> ExecuteAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var person = await unitOfWork.Persons.GetByIdAsync(personId, cancellationToken);
        if (person is null)
        {
            return null;
        }

        var classified = await unitOfWork.Purchases.ListClassifiedAsync(cancellationToken);

        var purchases = classified
            .Select(purchase => (
                Purchase: purchase,
                Share: purchase.Shares.FirstOrDefault(share => share.PersonId == personId)))
            .Where(item => item.Share is not null)
            .OrderByDescending(item => item.Purchase.Date)
            .ToList();

        var purchaseItems = purchases
            .Select(item => new PersonPurchaseItem(
                item.Purchase.Id,
                item.Purchase.Merchant?.DisplayName ?? item.Purchase.Description,
                item.Purchase.Date,
                item.Purchase.AmountCents,
                item.Share!.AmountCents,
                item.Purchase.Shares.Count > 1,
                item.Purchase.Shares.Count))
            .ToList();

        var cardTotals = purchases
            .GroupBy(item => new { item.Purchase.CardId, CardName = item.Purchase.Card?.Name ?? "Cartão" })
            .Select(group => new PersonCardTotal(
                group.Key.CardId,
                group.Key.CardName,
                group.Sum(item => item.Share!.AmountCents)))
            .OrderByDescending(total => total.TotalCents)
            .ToList();

        return new PersonDetail(
            person.Id,
            person.Name,
            person.Color,
            purchaseItems.Sum(item => item.PersonShareCents),
            cardTotals,
            purchaseItems);
    }
}
