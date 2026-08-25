using GerenciadorDeFinancas.Application.Dtos;
using GerenciadorDeFinancas.Domain.Abstractions;
using GerenciadorDeFinancas.Domain.Enums;
using GerenciadorDeFinancas.Domain.ValueObjects;

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

        var today = DateOnly.FromDateTime(DateTime.Today);
        var cards = await unitOfWork.Cards.ListActiveAsync(cancellationToken);
        var cardTotals = new List<PersonCardTotal>();
        var currentInvoicePurchases = new List<Domain.Entities.Purchase>();

        foreach (var card in cards)
        {
            var period = StatementPeriodCalculator.Calculate(card.ClosingDay, today);
            var statement = await unitOfWork.Statements.GetByCardAndYearMonthAsync(card.Id, period.YearMonth, cancellationToken);
            var invoicePurchases = statement?.Purchases
                .Where(purchase => purchase.Status == PurchaseStatus.Classified)
                .ToList() ?? [];

            currentInvoicePurchases.AddRange(invoicePurchases);

            var personTotal = invoicePurchases
                .SelectMany(purchase => purchase.Shares)
                .Where(share => share.PersonId == personId)
                .Sum(share => share.AmountCents);

            if (personTotal > 0)
            {
                cardTotals.Add(new PersonCardTotal(card.Id, card.Name, personTotal));
            }
        }

        var purchaseItems = currentInvoicePurchases
            .Select(purchase => (
                Purchase: purchase,
                Share: purchase.Shares.FirstOrDefault(share => share.PersonId == personId)))
            .Where(item => item.Share is not null)
            .OrderByDescending(item => item.Purchase.Date)
            .Select(item => new PersonPurchaseItem(
                item.Purchase.Id,
                item.Purchase.Merchant?.DisplayName ?? item.Purchase.Description,
                item.Purchase.Date,
                item.Purchase.AmountCents,
                item.Share!.AmountCents,
                item.Purchase.Shares.Count > 1,
                item.Purchase.Shares.Count))
            .ToList();

        return new PersonDetail(
            person.Id,
            person.Name,
            person.Color,
            cardTotals.Sum(total => total.TotalCents),
            cardTotals,
            purchaseItems);
    }
}
